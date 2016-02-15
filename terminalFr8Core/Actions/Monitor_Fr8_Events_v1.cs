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
using Data.States;
using System.Globalization;

namespace terminalFr8Core.Actions
{
    public class Monitor_Fr8_Events_v1 : BaseTerminalActivity
    {
        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActivityDO, ConfigurationEvaluator, authTokenDO);
        }

        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            //build a controls crate to render the pane
            var eventSubscription = PackCrate_EventSubscriptions();

            var textBlock = GenerateTextBlock("Monitor Fr8 Events",
                "This Activity doesn't require any configuration.", "well well-lg");
            var curControlsCrate = PackControlsCrate(textBlock);

            var routeActivatedCrate = Crate.CreateManifestDescriptionCrate("Available Run-Time Objects", "RouteActivated", "13", AvailabilityType.RunTime);
            var routeDeactivatedCrate = Crate.CreateManifestDescriptionCrate("Available Run-Time Objects", "RouteDeactivated", "13", AvailabilityType.RunTime);
            var containerLaunched = Crate.CreateManifestDescriptionCrate("Available Run-Time Objects", "ContainerLaunched", "13", AvailabilityType.RunTime);
            var containerExecutionComplete = Crate.CreateManifestDescriptionCrate("Available Run-Time Objects", "ContainerExecutionComplete", "13", AvailabilityType.RunTime);
            var actionExecuted = Crate.CreateManifestDescriptionCrate("Available Run-Time Objects", "ActionExecuted", "13", AvailabilityType.RunTime);

            using (var crateStorage = Crate.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Add(curControlsCrate);
                crateStorage.Add(routeActivatedCrate);
                crateStorage.Add(routeDeactivatedCrate);
                crateStorage.Add(containerLaunched);
                crateStorage.Add(containerExecutionComplete);
                crateStorage.Add(actionExecuted);
                crateStorage.Add(eventSubscription);
            }

            return await Task.FromResult(curActivityDO);
        }

        protected override Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            return Task.FromResult(curActivityDO);
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);
            var curEventReport = Crate.GetStorage(payloadCrates).CrateContentsOfType<EventReportCM>().First();

            if (curEventReport != null)
            {
                var standardLoggingCM = curEventReport.EventPayload.CrateContentsOfType<StandardLoggingCM>().First();

                if (standardLoggingCM != null)
                {
                    using (var crateStorage = Crate.GetUpdatableStorage(payloadCrates))
                    {
                        crateStorage.Add(Data.Crates.Crate.FromContent(curEventReport.EventNames, standardLoggingCM));
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
                "Fr8Core",
                subscriptions.ToArray()
                );
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            if (Crate.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }
    }
}