﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using PhoneNumbers;
using StructureMap;
using terminalTwilio.Services;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Twilio;

namespace terminalTwilio.Actions
{
    public class Send_Via_Twilio_v1 : BaseTerminalActivity
    {
        protected ITwilioService _twilio;

        public Send_Via_Twilio_v1()
        {
            _twilio = ObjectFactory.GetInstance<ITwilioService>();
        }

        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActivityDO, EvaluateReceivedRequest, authTokenDO);
        }

        /// <summary>
        /// this entire function gets passed as a delegate to the main processing code in the base class
        /// currently many actions have two stages of configuration, and this method determines which stage should be applied
        /// </summary>
        private ConfigurationRequestType EvaluateReceivedRequest(ActivityDO curActivityDO)
        {
            if (CrateManager.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Clear();
                crateStorage.Add(PackCrate_ConfigurationControls());
                crateStorage.Add(await CreateAvailableFieldsCrate(curActivityDO));
            }
            return await Task.FromResult(curActivityDO);
        }

        private Crate PackCrate_ConfigurationControls()
        {
            var fieldsDTO = new List<ControlDefinitionDTO>()
            {
                CreateSpecificOrUpstreamValueChooser("SMS Number", "SMS_Number", "Upstream Terminal-Provided Fields"),
                CreateSpecificOrUpstreamValueChooser("SMS Body", "SMS_Body", "Upstream Terminal-Provided Fields")
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }

        private List<FieldDTO> GetRegisteredSenderNumbersData()
        {
            return _twilio.GetRegisteredSenderNumbers().Select(number => new FieldDTO() { Key = number, Value = number }).ToList();
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.RemoveByLabel("Upstream Terminal-Provided Fields");
                crateStorage.Add(await CreateAvailableFieldsCrate(curActivityDO));
            }
            return curActivityDO;
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            Message curMessage;
            var payloadCrates = await GetPayload(curActivityDO, containerId);
            var controlsCrate = CrateManager.GetStorage(curActivityDO).CratesOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            if (controlsCrate == null)
            {
                PackCrate_WarningMessage(curActivityDO, "No StandardConfigurationControlsCM crate provided", "No Controls");
                return Error(payloadCrates, "No StandardConfigurationControlsCM crate provided");
            }
            try
            {
                FieldDTO smsFieldDTO = ParseSMSNumberAndMsg(controlsCrate, payloadCrates);
                string smsNumber = smsFieldDTO.Key;
                string smsBody = smsFieldDTO.Value + " - https://fr8.co/c/"+containerId;

                try
                {
                    curMessage = _twilio.SendSms(smsNumber, smsBody);
                    EventManager.TwilioSMSSent(smsNumber, smsBody);
                    var curFieldDTOList = CreateKeyValuePairList(curMessage);
                    using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
                    {
                        crateStorage.Add(PackCrate_TwilioMessageDetails(curFieldDTOList));
                    }
                }
                catch (Exception ex)
                {
                    EventManager.TwilioSMSSendFailure(smsNumber, smsBody, ex.Message);
                    PackCrate_WarningMessage(curActivityDO, ex.Message, "Twilio Service Failure");
                    return Error(payloadCrates, "Twilio Service Failure");
                }
            }
            catch (ArgumentException appEx)
            {
                PackCrate_WarningMessage(curActivityDO, appEx.Message, "SMS Number");
                return Error(payloadCrates, appEx.Message);
            }
            
            return Success(payloadCrates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="crateDTO"></param>
        /// <returns>Key = SMS Number; Value = SMS Body</returns>
        public FieldDTO ParseSMSNumberAndMsg(Crate crateDTO, PayloadDTO payloadCrates)
        {
            var standardControls = crateDTO.Get<StandardConfigurationControlsCM>();
            if (standardControls == null)
            {
                throw new ArgumentException("CrateDTO is not a standard UI control");
            }
            var payloadCrateStorage = CrateManager.GetStorage(payloadCrates);
            var smsNumber = GetSMSNumber((TextSource)standardControls.Controls[0], payloadCrateStorage);
            var smsBody = GetSMSBody((TextSource)standardControls.Controls[1], payloadCrateStorage);

            return new FieldDTO(smsNumber, smsBody);
        }

        public override Task ValidateActivity(ActivityDO curActivityDO, ICrateStorage crateStorage, ValidationManager validationManager)
        {
            var configControl = GetConfigurationControls(crateStorage);

            if (configControl != null)
            {
                var numberControl = (TextSource)configControl.Controls[0];
                var bodyControl = (TextSource)configControl.Controls[1];

                if (numberControl != null)
                {
                    var smsNumber = numberControl.TextValue;

                    ValidateSMSNumber(validationManager, smsNumber, numberControl);
                }

                if (bodyControl != null)
                {
                    if (bodyControl.TextValue == null && bodyControl.Value == null)
                    {
                        validationManager.SetError("SMS body can not be null.", bodyControl);
                    }
                }
            }

            return Task.FromResult(0);
        }
        

        private string GetSMSNumber(TextSource control, ICrateStorage payloadCrates)
        {
            string smsNumber = "";
            if (control == null)
            {
                throw new ApplicationException("TextSource control was expected but not found.");
            }
            smsNumber = control.GetValue(payloadCrates).Trim();
            if (smsNumber.Length == 10 && !smsNumber.Contains("+"))
                smsNumber = "+1" + smsNumber;

            return smsNumber;
        }

        private string GetSMSBody(TextSource control, ICrateStorage payloadCrates)
        {
            string smsBody = "";
            if (control == null)
            {
                throw new ApplicationException("TextSource control was expected but not found.");
            }

            smsBody = control.GetValue(payloadCrates);
            if (smsBody == null)
            {
                throw new ArgumentException("SMS body can not be null.");
            }

            smsBody = "Fr8 Alert: " + smsBody + " For more info, visit http://fr8.co/sms";

            return smsBody;
        }

        private List<FieldDTO> CreateKeyValuePairList(Message curMessage)
        {
            List<FieldDTO> returnList = new List<FieldDTO>();
            returnList.Add(new FieldDTO("Status", curMessage.Status));
            returnList.Add(new FieldDTO("ErrorMessage", curMessage.ErrorMessage));
            returnList.Add(new FieldDTO("Body", curMessage.Body));
            returnList.Add(new FieldDTO("ToNumber", curMessage.To));
            return returnList;
        }
        private Crate PackCrate_TwilioMessageDetails(List<FieldDTO> curTwilioMessage)
        {
            return Data.Crates.Crate.FromContent("Message Data", new StandardPayloadDataCM(curTwilioMessage));
        }

        private void PackCrate_WarningMessage(ActivityDO activityDO, string warningMessage, string warningLabel)
        {
            var textBlock = GenerateTextBlock(warningLabel, warningMessage, "alert alert-warning");
            using (var crateStorage = CrateManager.GetUpdatableStorage(activityDO))
            {
                crateStorage.Clear();
                crateStorage.Add(PackControlsCrate(textBlock));
            }
        }

        private bool ValidateSMSNumber(ValidationManager validationManager, string smsNumber, ControlDefinitionDTO control)
        {
            try
            {
                if (String.IsNullOrEmpty(smsNumber))
                {
                    validationManager.SetError("No SMS Number Provided", control);
                    return false;
                }

                smsNumber = smsNumber.Trim();
                if (smsNumber.Length == 10 && !smsNumber.Contains("+"))
                    smsNumber = "+1" + smsNumber;

                PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
                bool isAlphaNumber = phoneUtil.IsAlphaNumber(smsNumber);
                PhoneNumber phoneNumber = phoneUtil.Parse(smsNumber, "");
                if (isAlphaNumber || !phoneUtil.IsValidNumber(phoneNumber))
                {
                    validationManager.SetError("SMS Number Is Invalid", control);
                    return false;
                }
            }
            catch (NumberParseException npe)
            {
                validationManager.SetError("Failed to parse SMS number: " + npe.Message, control);
                return false;
            }

            return true;
        }
    }
}