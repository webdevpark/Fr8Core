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
using terminalDocuSignTests.Fixtures;

namespace terminalDocuSignTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    [Category("terminalDocuSignTests.Integration")]
    public class Mail_Merge_Into_DocuSign_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        private void AssertCrateTypes(ICrateStorage crateStorage)
        {
            Assert.AreEqual(2, crateStorage.Count);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Available Templates"));
        }

        private void AddHubActivityTemplate(Fr8DataDTO dataDTO)
        {

            AddActivityTemplate(
              dataDTO,
              new ActivityTemplateDTO()
              {
                  Id = 1,
                  Name = "Load Excel File",
                  Label = "Load Excel File",
                  Tags = "Table Data Generator"
              }
          );

            AddActivityTemplate(
                dataDTO,
                new ActivityTemplateDTO()
                {
                    Id = 2,
                    Name = "Extract Spreadsheet Data",
                    Label = "Extract Spreadsheet Data",
                    Tags = "Table Data Generator"
                }
            );

            AddActivityTemplate(
               dataDTO,
               new ActivityTemplateDTO()
               {
                   Id = 3,
                   Name = "Get Google Sheet Data",
                   Label = "Get Google Sheet Data",
                   Tags = "Table Data Generator",
                   Category = Data.States.ActivityCategory.Receivers
               }
           );
        }

        private void AssertControls(StandardConfigurationControlsCM controls)
        {
            // Assert that DataSource ,  DocuSignTemplate and Button control are present
            Assert.AreEqual(3, controls.Controls.Count());
            Assert.AreEqual(1, controls.Controls.Count(x => x.Name == "DataSource"));
            Assert.AreEqual(1, controls.Controls.Count(x => x.Name == "DocuSignTemplate"));
            Assert.AreEqual(1, controls.Controls.Count(x => x.Type == "Button"));

            // Assert that DataSource dropdown contains sources and it should be only "Get"
            var dataSourceDropdown = (DropDownList)controls.Controls[0];
            Assert.AreEqual(1, dataSourceDropdown.ListItems.Count());
            Assert.IsFalse(dataSourceDropdown.ListItems.Any(x => !x.Key.StartsWith("Get", StringComparison.InvariantCultureIgnoreCase)));

            // Assert that Dropdownlist  with source labeled "Available Templates".
            var templateDropdown = (DropDownList)controls.Controls[1];
            Assert.AreEqual("Available Templates", templateDropdown.Source.Label);
        }

        private async Task<ActivityDTO> GetActionDTO_WithDataStorage(string childAction)
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            AddHubActivityTemplate(requestDataDTO);
            
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );
            
            responseActionDTO.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();

            using (var crateStorage = Crate.GetUpdatableStorage(responseActionDTO))
            {
                var controls = crateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .Single();

                var dataSourceDropdown = (DropDownList)controls.Controls[0];
                dataSourceDropdown.Value = childAction;

                var availableTemplatesCM = crateStorage
                  .CrateContentsOfType<StandardDesignTimeFieldsCM>(x => x.Label == "Available Templates")
                  .Single();
                Assert.IsTrue(availableTemplatesCM.Fields.Count > 0);

                var templateDropdown = (DropDownList)controls.Controls[1];
                templateDropdown.Value = availableTemplatesCM.Fields[0].Value;

                var continueButton = (Button)controls.Controls[2];
                continueButton.Clicked = true;
            }

            return responseActionDTO;
        }

        [Test]
        public async Task Mail_Merge_Into_DocuSign_Initial_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            AddHubActivityTemplate(requestDataDTO);

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertCrateTypes(crateStorage);
            AssertControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        [Test]
        public void Mail_Merge_Into_DocuSign_FollowUp_Configuration_Check_ChildAction_Load_Excel_File()
        {
            //string childAction = "Load Excel File";
            //var configureUrl = GetTerminalConfigureUrl();

            //var requestActionDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_ActionDTO();

            //var responseActionDTO = await GetActionDTO_WithDataStorage(childAction);
            // responseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
            //        configureUrl,
            //        responseActionDTO
            //    );
          
            //Assert.NotNull(responseActionDTO);
            //Assert.NotNull(responseActionDTO.CrateStorage);
            //Assert.NotNull(responseActionDTO.CrateStorage.Crates);
            //Assert.AreEqual(1, responseActionDTO.ChildrenActions.Length);

            //// Assert that Selected child Action is present
            //Assert.AreEqual(1, responseActionDTO.ChildrenActions.Count(x=> x.Label == "Load Excel File"));
        }

        [Test]
        public void Mail_Merge_Into_DocuSign_FollowUp_Configuration_Check_ChildAction_Extract_Spreadsheet_Data()
        {
            //string childAction = "Extract Spreadsheet Data";
            //var configureUrl = GetTerminalConfigureUrl();

            //var requestActionDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_ActionDTO();

            //var responseActionDTO = await GetActionDTO_WithDataStorage(childAction);
            //responseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
            //       configureUrl,
            //       responseActionDTO
            //   );

            //Assert.NotNull(responseActionDTO);
            //Assert.NotNull(responseActionDTO.CrateStorage);
            //Assert.NotNull(responseActionDTO.CrateStorage.Crates);
            //Assert.AreEqual(1, responseActionDTO.ChildrenActions.Length);

            //// Assert that Selected child Action is present
            //Assert.AreEqual(1, responseActionDTO.ChildrenActions.Count(x => x.Label == "Extract Spreadsheet Data"));
        }

        [Test]
        public async Task Mail_Merge_Into_DocuSign_Activate_Returns_ActionDTO()
        {
            //Arrange
            var configureUrl = GetTerminalActivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActionDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            //Act
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }

        [Test]
        public async Task Mail_Merge_Into_DocuSign_Deactivate_Returns_ActionDTO()
        {
            //Arrange
            var configureUrl = GetTerminalDeactivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActionDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            //Act
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }
    }
}
