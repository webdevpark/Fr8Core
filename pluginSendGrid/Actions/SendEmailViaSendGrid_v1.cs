﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginBase.Infrastructure;
using StructureMap;
using PluginBase;
using PluginBase.BaseClasses;
using Core.Interfaces;
using Core.Services;
using Core.StructureMap;
using Data.States.Templates;
using Data.Interfaces.ManifestSchemas;
using pluginSendGrid.Infrastructure;
using Data.Interfaces;
using pluginSendGrid.Services;

namespace pluginSendGrid.Actions
{
    public class SendEmailViaSendGrid_v1 : BasePluginAction
    {
        private IAction _action;
        private ICrate _crate;
        private IEmailPackager _emailPackager;

        public SendEmailViaSendGrid_v1()
        {
            _action = ObjectFactory.GetInstance<IAction>();
            _crate = ObjectFactory.GetInstance<ICrate>();
            _emailPackager = new SendGridPackager();
        }

        //================================================================================
        //General Methods (every Action class has these)

        //maybe want to return the full Action here
        public ActionDTO Configure(ActionDTO curActionDTO)
        {
            return ProcessConfigurationRequest(curActionDTO, EvaluateReceivedRequest);
        }

        //this entire function gets passed as a delegate to the main processing code in the base class
        //currently many actions have two stages of configuration, and this method determines which stage should be applied
        private ConfigurationRequestType EvaluateReceivedRequest(ActionDTO curActionDTO)
        {
            CrateStorageDTO curCrates = curActionDTO.CrateStorage;

            if (curCrates.CrateDTO.Count == 0)
                return ConfigurationRequestType.Initial;
            else
                return ConfigurationRequestType.Followup;
        }

        protected override ActionDTO InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            if (curActionDTO.CrateStorage == null)
            {
                curActionDTO.CrateStorage = new CrateStorageDTO();
            }
            curActionDTO.CrateStorage.CrateDTO.Add(CreateControlsCrate());
            curActionDTO.CrateStorage.CrateDTO.Add(GetAvailableDataFields(curActionDTO));
            return curActionDTO;
        }

        private CrateDTO CreateControlsCrate()
        {

            FieldDefinitionDTO[] controls = 
            {
                new FieldDefinitionDTO("textField")
                {
                        Label = "Recipient Email Address",
                        Name = "Recipient_Email_Address",
                        Required = false
                },
                new FieldDefinitionDTO("textField")
                {
                        Label = "Email Subject",
                        Name = "Email_Subject",
                        Required = false
                },
                new FieldDefinitionDTO("textField")
                {
                        Label = "Email Body",
                        Name = "Email_Body",
                        Required = false
                }
            };
            return _crate.CreateStandardConfigurationControlsCrate("Send Grid", controls);
        }

        private CrateDTO GetAvailableDataFields(ActionDTO curActionDTO)
        {
            CrateDTO crateDTO = null;
            ActionDO curActionDO = null;
            try
            {
               curActionDO = _action.MapFromDTO(curActionDTO);
            }
            catch (Exception ex)
            {

                throw;
            }
            var curUpstreamFields = GetDesignTimeFields(curActionDO, GetCrateDirection.Upstream).Fields.ToArray();

            if (curUpstreamFields.Length == 0)
            {
                crateDTO = GetTextBoxControlForDisplayingError("MapFieldsErrorMessage",
                         "This action couldn't find either source fields or target fields (or both). " +
                        "Try configuring some Actions first, then try this page again.");
                curActionDTO.CurrentView = "MapFieldsErrorMessage";
            }
            else
            {
                crateDTO = _crate.CreateDesignTimeFieldsCrate("Upstream Plugin-Provided Fields", curUpstreamFields);
            }

            return crateDTO;
        }

        protected override ActionDTO FollowupConfigurationResponse(ActionDTO curActionDTO)
        {
            //not currently any requirements that need attention at FollowupConfigurationResponse
            return null;
        }

        public object Activate(ActionDO curActionDO)
        {
            //not currently any requirements that need attention at Activation Time
            return null;
        }

        public object Deactivate(ActionDO curActionDO)
        {
            return null;
        }

        public object Execute(ActionDataPackageDTO curActionDataPackage)
        {
            var mailerDO = AutoMapper.Mapper.Map<IMailerDO>(curActionDataPackage.PayloadDTO);

            _emailPackager.Send(mailerDO);

            return true;
        }
    }
}