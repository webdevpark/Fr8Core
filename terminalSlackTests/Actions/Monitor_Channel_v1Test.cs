﻿using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using terminalSlackTests.Fixtures;
using UtilitiesTesting;
using Newtonsoft.Json;

namespace terminalSlackTests.Actions
{
    //https://maginot.atlassian.net/wiki/display/DDW/Monitor_Channel_v1+test+case

    [Explicit]
    public class Monitor_Channel_v1Test : BaseHealthMonitorTest
    {
        public override string TerminalName
        {
            get { return "terminalSlack"; }
        }

        [Test]
        public async void Monitor_Channel_Initial_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Monitor_Channel_v1_InitialConfiguration_ActionDTO();

            var responseActionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    requestActionDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertCrateTypes(crateStorage);
            AssertControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        private void AssertControls(StandardConfigurationControlsCM controls)
        {
            Assert.AreEqual(2, controls.Controls.Count);

            //DDLB test
            Assert.IsTrue(controls.Controls[0] is DropDownList);
            Assert.AreEqual("Selected_Slack_Channel", controls.Controls[0].Name);
            Assert.AreEqual(1, controls.Controls[0].Events.Count);
            Assert.AreEqual("onChange", controls.Controls[0].Events[0].Name);
            Assert.AreEqual("requestConfig", controls.Controls[0].Events[0].Handler);

            Assert.IsTrue(controls.Controls[1] is TextBlock);
            Assert.AreEqual("Info_Label", controls.Controls[1].Name);
        }

        private void AssertCrateTypes(CrateStorage crateStorage)
        {
            Assert.AreEqual(4, crateStorage.Count);

            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Available Fields"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Available Channels"));
            Assert.AreEqual(1, crateStorage.CratesOfType<EventSubscriptionCM>().Count(x => x.Label == "Standard Event Subscriptions"));
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(RestfulServiceException))]
        public async void Monitor_Channel_Initial_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Monitor_Channel_v1_InitialConfiguration_ActionDTO();
            requestActionDTO.AuthToken = null;

            await HttpPostAsync<ActionDTO, Newtonsoft.Json.Linq.JToken>(
                configureUrl,
                requestActionDTO
            );
        }

        [Test]
        public async void Monitor_Channel_Run_RightChannel_Test()
        {
            var runUrl = GetTerminalRunUrl();

            ActionDTO actionDTO = await GetConfiguredActionWithDDLBSelected("slack-plugin-test");

            var responsePayloadDTO =
             await HttpPostAsync<ActionDTO, PayloadDTO>(runUrl, actionDTO);

            var crateStorage = Crate.GetStorage(responsePayloadDTO);

            Assert.AreEqual(1, crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "Slack Payload Data").Count());
            var slackPayload = crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "Slack Payload Data").Single();

            Assert.AreEqual(1, slackPayload.AllValues().Where(a => a.Key == "text" && a.Value == "test").Count());
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = "{\"status\":\"terminal_error\",\"message\":\"Unexpected channel-id.\"}")]
        public async void Monitor_Channel_Run_WrongChannel_Test()
        {
            var runUrl = GetTerminalRunUrl();
            var actionDTO = await GetConfiguredActionWithDDLBSelected("dev");
            var responsePayloadDTO =
               await HttpPostAsync<ActionDTO, PayloadDTO>(runUrl, actionDTO);
        }

        private async Task<ActionDTO> GetConfiguredActionWithDDLBSelected(string selectedChannel)
        {
            var configureUrl = GetTerminalConfigureUrl();
            var requestActionDTO = HealthMonitor_FixtureData.Monitor_Channel_v1_InitialConfiguration_ActionDTO();
            var actionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    requestActionDTO
                );

            actionDTO.AuthToken = HealthMonitor_FixtureData.Slack_AuthToken();
            AddPayloadCrate(
                actionDTO,
                 new EventReportCM()
                 {
                     EventPayload = new CrateStorage()
                    {
                        Data.Crates.Crate.FromContent(
                            "EventReport",
                            new StandardPayloadDataCM(HealthMonitor_FixtureData.SlackEventFields()
                            )
                        )
                    }
                 }
            );
            using (var updater = Crate.UpdateStorage(actionDTO))
            {
                var controls = updater.CrateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .Single();

                terminalSlack.Services.SlackIntegration slackIntegraion = new terminalSlack.Services.SlackIntegration();
                var channels = await slackIntegraion.GetChannelList(HealthMonitor_FixtureData.Slack_AuthToken().Token);

                var ddlb = (DropDownList)controls.Controls[0];
                ddlb.Value = channels.Where(a => a.Key == selectedChannel).FirstOrDefault().Value;
            }

            return actionDTO;
        }
    }
}
