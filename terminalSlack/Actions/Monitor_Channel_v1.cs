﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using TerminalBase.Infrastructure;
using terminalSlack.Interfaces;
using terminalSlack.Services;
using TerminalBase.BaseClasses;
using Data.Entities;

namespace terminalSlack.Actions
{
    public class Monitor_Channel_v1 : BasePluginAction
    {
        private readonly ISlackIntegration _slackIntegration;

        public Monitor_Channel_v1()
        {
            _slackIntegration = new SlackIntegration();
        }

        public async Task<PayloadDTO> Run(ActionDO actionDO, int containerId, AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            var processPayload = await GetProcessPayload(containerId);
            var payloadFields = ExtractPayloadFields(processPayload);

            var payloadChannelIdField = payloadFields.FirstOrDefault(x => x.Key == "channel_id");
            if (payloadChannelIdField == null)
            {
                throw new ApplicationException("No channel_id field found in payload.");
            }

            var payloadChannelId = payloadChannelIdField.Value;
            var actionChannelId = ExtractControlFieldValue(actionDO, "Selected_Slack_Channel");

            if (payloadChannelId != actionChannelId)
            {
                throw new ApplicationException("Unexpected channel-id.");
            }

            var cratePayload = Crate.Create(
                "Slack Payload Data",
                JsonConvert.SerializeObject(payloadFields),
                CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME,
                CrateManifests.STANDARD_PAYLOAD_MANIFEST_ID
                );

            processPayload.UpdateCrateStorageDTO(new List<CrateDTO>() { cratePayload });

            return processPayload;
        }

        private List<FieldDTO> ExtractPayloadFields(PayloadDTO processPayload)
        {
            var eventReportCrate = processPayload.CrateStorageDTO()
                .CrateDTO
                .SingleOrDefault();
            if (eventReportCrate == null)
            {
                throw new ApplicationException("EventReportCrate is empty.");
            }

            var eventReportMS = JsonConvert.DeserializeObject<EventReportCM>(
                eventReportCrate.Contents);
            var eventFieldsCrate = eventReportMS.EventPayload.SingleOrDefault();
            if (eventFieldsCrate == null)
            {
                throw new ApplicationException("EventReportMS.EventPayload is empty.");
            }

            var payloadFields = JsonConvert.DeserializeObject<List<FieldDTO>>(eventFieldsCrate.Contents);

            return payloadFields;
        }

        public override async Task<ActionDO> Configure(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActionDO, x => ConfigurationEvaluator(x),authTokenDO);
        }

        private ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            var crateStorage = curActionDO.CrateStorageDTO();

            if (crateStorage.CrateDTO.Count == 0)
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            var oauthToken = authTokenDO.Token;
            var channels = await _slackIntegration.GetChannelList(oauthToken);

            var crateControls = PackCrate_ConfigurationControls();
            var crateDesignTimeFields = CreateDesignTimeFieldsCrate();
            var crateAvailableChannels = CreateAvailableChannelsCrate(channels);
            var crateEventSubscriptions = CreateEventSubscriptionCrate();
            var curCrateDTOList = new List<CrateDTO>();
            curCrateDTOList.Add(crateControls);
            curCrateDTOList.Add(crateDesignTimeFields);
            curCrateDTOList.Add(crateAvailableChannels);
            curCrateDTOList.Add(crateEventSubscriptions);
            curActionDO.UpdateCrateStorageDTO(curCrateDTOList);

            return await Task.FromResult<ActionDO>(curActionDO);
        }

        private CrateDTO PackCrate_ConfigurationControls()
        {
            var fieldSelectChannel = new DropDownListControlDefinitionDTO()
            {
                Label = "Select Slack Channel",
                Name = "Selected_Slack_Channel",
                Required = true,
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                },
                Source = new FieldSourceDTO
                {
                    Label = "Available Channels",
                    ManifestType = CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                }
            };

            return PackControlsCrate(fieldSelectChannel);
        }

        private CrateDTO CreateDesignTimeFieldsCrate()
        {
            var fields = new List<FieldDTO>()
            {
                new FieldDTO() { Key = "token", Value = "token" },
                new FieldDTO() { Key = "team_id", Value = "team_id" },
                new FieldDTO() { Key = "team_domain", Value = "team_domain" },
                new FieldDTO() { Key = "service_id", Value = "service_id" },
                new FieldDTO() { Key = "timestamp", Value = "timestamp" },
                new FieldDTO() { Key = "channel_id", Value = "channel_id" },
                new FieldDTO() { Key = "channel_name", Value = "channel_name" },
                new FieldDTO() { Key = "user_id", Value = "user_id" },
                new FieldDTO() { Key = "user_name", Value = "user_name" },
                new FieldDTO() { Key = "text", Value = "text" }
            };

            var crate =
                Crate.CreateDesignTimeFieldsCrate(
                    "Available Fields",
                    fields.ToArray()
                );

            return crate;
        }

        private CrateDTO CreateAvailableChannelsCrate(IEnumerable<FieldDTO> channels)
        {
            var crate =
                Crate.CreateDesignTimeFieldsCrate(
                    "Available Channels",
                    channels.ToArray()
                );

            return crate;
        }

        private CrateDTO CreateEventSubscriptionCrate()
        {
            var subscriptions = new string[] {
                "Slack Outgoing Message"
            };

            return Crate.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                subscriptions.ToArray()
                );
        }
    }
}