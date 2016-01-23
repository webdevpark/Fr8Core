using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Constants;
using Data.Crates;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using TerminalBase.Infrastructure;
using TerminalBase.BaseClasses;
using Data.Entities;
using StructureMap;
using Hub.Managers;
using Data.Control;

namespace terminalFr8Core.Actions
{
    public class Monitor_Fr8_Events_v1 : BaseTerminalAction
    {
        public override async Task<ActionDO> Configure(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActionDO, ConfigurationEvaluator, authTokenDO);
        }

        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            //build a controls crate to render the pane
            var eventFields = Crate.CreateDesignTimeFieldsCrate("Monitor Fr8 Event Fields", CreateMonitorFr8EventFields().ToArray());
            var eventSubscription = PackCrate_EventSubscriptions();

            var textBlock = GenerateTextBlock("Monitor Fr8 Events",
                "This Action doesn't require any configuration.", "well well-lg");
            var curControlsCrate = PackControlsCrate(textBlock);

            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage.Add(curControlsCrate);
                updater.CrateStorage.Add(eventFields);
                updater.CrateStorage.Add(eventSubscription);
            }

            return await Task.FromResult(curActionDO);
        }

        protected override Task<ActionDO> FollowupConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            return Task.FromResult(curActionDO);
        }

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActionDO, containerId);
            var curEventReport = Crate.GetStorage(payloadCrates).CrateContentsOfType<EventReportCM>().First();

            if (curEventReport != null)
            {
                var standardLoggingCM = curEventReport.EventPayload.CrateContentsOfType<StandardLoggingCM>().First();

                if (standardLoggingCM != null)
                {
                    using (var updater = Crate.UpdateStorage(payloadCrates))
                    {
                        updater.CrateStorage.Add(Data.Crates.Crate.FromContent(curEventReport.EventNames, standardLoggingCM));
                    }
                }
            }

            return Success(payloadCrates);
        }

        private Crate PackCrate_EventSubscriptions()
        {
            var subscriptions = new List<string>();
            subscriptions.Add("RouteActivated");
            subscriptions.Add("RouteDeactivated");
            subscriptions.Add("ContainerLaunched");
            subscriptions.Add("ContainerExecutionComplete");
            subscriptions.Add("ActionExecuted");

            return Crate.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                subscriptions.ToArray()
                );
        }

        private List<FieldDTO> CreateMonitorFr8EventFields()
        {
            return new List<FieldDTO>(){
                new FieldDTO("RouteActivated",string.Empty),
                new FieldDTO("RouteDeactivated",string.Empty),
                new FieldDTO("ContainerLaunched",string.Empty),
                new FieldDTO("ContainerExecutionComplete",string.Empty),
                new FieldDTO("ActionExecuted",string.Empty),
            };
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            if (Crate.IsStorageEmpty(curActionDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }
    }
}