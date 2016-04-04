﻿using System;
using NUnit.Framework;
using Data.Interfaces.DataTransferObjects;
using HealthMonitor.Utility;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Data.Constants;
using Hub.Managers;
using Data.Interfaces.Manifests;
using terminalDocuSignTests.Fixtures;
using Newtonsoft.Json.Linq;
using Data.Crates;
using Data.Control;
using Data.States;
using Data.Entities;
using DocuSign.eSign.Api;
using terminaBaselTests.Tools.Activities;
using terminalDocuSign.Services;
using terminalDocuSign.Services.New_Api;
using UtilitiesTesting.Fixtures;

namespace terminalDocuSignTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    [Category("terminalDocuSignTests.Integration")]
    public class Mail_Merge_Into_DocuSign_v1_EndToEnd_Tests : BaseHubIntegrationTest
    {
        #region Properties

        private ActivityDTO solution;
        private ICrateStorage crateStorage;
        private terminaBaselTests.Tools.Terminals.IntegrationTestTools_terminalDocuSign _terminalDocuSignTestTools;

        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        #endregion

        public Mail_Merge_Into_DocuSign_v1_EndToEnd_Tests()
        {
            _terminalDocuSignTestTools = new terminaBaselTests.Tools.Terminals.IntegrationTestTools_terminalDocuSign(this);
        }
        

        [Test]
        [ExpectedException(typeof(AssertionException))]
        public async Task TestEmail_ShouldBeMissing()
        {
            await Task.Delay(EmailAssert.RecentMsgThreshold); //to avoid false positives from any earlier tests

            // Verify that test email has been received
            // Actually it should not be received and AssertionException 
            // should be thrown
            EmailAssert.EmailReceived("dse_demo@docusign.net", "Test Message from Fr8");
        }

        [Test, Ignore]
        public async Task Mail_Merge_Into_DocuSign_EndToEnd()
        {
            await RevokeTokens();

            var googleAuthTokenId = await ExtractGoogleDefaultToken();

            var solutionCreateUrl = _baseUrl + "activities/create?solutionName=Mail_Merge_Into_DocuSign";

            //
            // Create solution
            //
            var plan = await HttpPostAsync<string, PlanDTO>(solutionCreateUrl, null);
            var solution = plan.Plan.SubPlans.FirstOrDefault().Activities.FirstOrDefault();

            //
            // Send configuration request without authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + solution.Id, solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            var stAuthCrate = crateStorage.CratesOfType<StandardAuthenticationCM>().FirstOrDefault();
            bool defaultDocuSignAuthTokenExists = stAuthCrate == null;

            if (!defaultDocuSignAuthTokenExists)
            {
                // Authenticate with DocuSign
                await _terminalDocuSignTestTools.AuthenticateDocuSignAndAccociateTokenWithAction(solution.Id, GetDocuSignCredentials(), solution.ActivityTemplate.TerminalId);
            }

            //
            // Send configuration request with authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + solution.Id, solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            Assert.True(crateStorage.CratesOfType<StandardConfigurationControlsCM>().Any(), "Crate StandardConfigurationControlsCM is missing in API response.");

            var controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var controls = controlsCrate.Content.Controls;
            var dataSource = controls.OfType<DropDownList>().FirstOrDefault(c => c.Name == "DataSource");
            dataSource.Value = "Get_Google_Sheet_Data";
            dataSource.selectedKey = "Get Google Sheet Data";
            var template = controls.OfType<DropDownList>().FirstOrDefault(c => c.Name == "DocuSignTemplate");
            template.Value = "9a4d2154-5b18-4316-9824-09432e62f458";
            template.selectedKey = "Medical_Form_v1";
            template.ListItems.Add(new ListItem() { Value = "9a4d2154-5b18-4316-9824-09432e62f458", Key = "Medical_Form_v1" });
            var button = controls.OfType<Button>().FirstOrDefault();
            button.Clicked = true;

            //
            //Rename route
            //
            var newName = plan.Plan.Name + " | " + DateTime.UtcNow.ToShortDateString() + " " +
                DateTime.UtcNow.ToShortTimeString();
            await HttpPostAsync<object, PlanFullDTO>(_baseUrl + "plans?id=" + plan.Plan.Id,
                new { id = plan.Plan.Id, name = newName });

            //
            // Configure solution
            //
            using (var crateStorage = Crate.GetUpdatableStorage(this.solution))
            {

                crateStorage.Remove<StandardConfigurationControlsCM>();
                crateStorage.Add(controlsCrate);
            }
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + this.solution.Id, this.solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            Assert.AreEqual(2, this.solution.ChildrenActivities.Count(), "Solution child actions failed to create.");

            // Assert Loop activity has CrateChooser with assigned manifest types.
            var loopActivity = this.solution.ChildrenActivities[1];
            using (var loopCrateStorage = Crate.GetUpdatableStorage(loopActivity))
            {
                var loopControlsCrate = loopCrateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
                var loopControls = loopControlsCrate.Content.Controls;

                var loopCrateChooser = loopControls
                    .Where(x => x.Type == ControlTypes.CrateChooser && x.Name == "Available_Crates")
                    .SingleOrDefault() as CrateChooser;

                Assert.NotNull(loopCrateChooser);
                Assert.AreEqual(1, loopCrateChooser.CrateDescriptions.Count);
                Assert.AreEqual("Standard Table Data", loopCrateChooser.CrateDescriptions[0].ManifestType);
                Assert.AreEqual("Table Generated From Google Sheet Data", loopCrateChooser.CrateDescriptions[0].Label);

                loopCrateChooser.CrateDescriptions = new List<CrateDescriptionDTO>();
            }

            // Delete Google action 
            await HttpDeleteAsync(_baseUrl + "activities?id=" + this.solution.ChildrenActivities[0].Id);

            // Add Add Payload Manually action
            var activityCategoryParam = new ActivityCategory[] { ActivityCategory.Processors };
            var activityTemplates = await HttpPostAsync<ActivityCategory[], List<WebServiceActivitySetDTO>>(_baseUrl + "webservices/activities", activityCategoryParam);
            var apmActivityTemplate = activityTemplates.SelectMany(a => a.Activities).Single(a => a.Name == "AddPayloadManually");

            var apmAction = new ActivityDTO()
            {
                ActivityTemplate = apmActivityTemplate,
                Label = apmActivityTemplate.Label,
                ParentPlanNodeId = this.solution.Id,
                RootPlanNodeId = plan.Plan.Id
            };
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", apmAction);
            Assert.NotNull(apmAction, "Add Payload Manually action failed to create");
            Assert.IsTrue(apmAction.Id != default(Guid), "Add Payload Manually action failed to create");

            //
            // Configure Add Payload Manually action
            //

            //Add rows to Add Payload Manually action
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure", apmAction);
            crateStorage = Crate.FromDto(apmAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var fieldList = controlsCrate.Content.Controls.OfType<FieldList>().First();
            fieldList.Value = @"[{""Key"":""Doctor"",""Value"":""Doctor1""},{""Key"":""Condition"",""Value"":""Condition1""}]";

            using (var updatableStorage = Crate.GetUpdatableStorage(apmAction))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(controlsCrate);
            }

            // Move Add Payload Manually action to the beginning of the plan
            apmAction.Ordering = 1;
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", apmAction);
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure", apmAction);
            Assert.AreEqual(1, apmAction.Ordering, "Failed to reoder the action Add Payload Manually");

            var fr8CoreLoop = this.solution.ChildrenActivities.Single(a => a.Label.Equals("loop", StringComparison.InvariantCultureIgnoreCase));

            using (var updatableStorage = Crate.UpdateStorage(() => fr8CoreLoop.CrateStorage))
            {
                var chooser = (CrateChooser)updatableStorage.CrateContentsOfType<StandardConfigurationControlsCM>().First().Controls.FirstOrDefault(c => c.Name == "Available_Crates");

                if (chooser?.CrateDescriptions != null)
                {
                    chooser.CrateDescriptions = new List<CrateDescriptionDTO>();
                }
            }
            
            fr8CoreLoop = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure", fr8CoreLoop);
            //we should update fr8Core loop to loop through manually added payload
            var fr8CoreLoopCrateStorage = Crate.FromDto(fr8CoreLoop.CrateStorage);
            var loopConfigCrate = fr8CoreLoopCrateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var loopConfigControls = loopConfigCrate.Content.Controls;
            var crateChooser = (CrateChooser)loopConfigControls.FirstOrDefault(c => c.Name == "Available_Crates");

            Assert.NotNull(crateChooser, "Crate chooser was not found");

            var payloadDataCrate = crateChooser.CrateDescriptions.SingleOrDefault(c => c.ManifestId == (int)MT.StandardPayloadData);

            Assert.NotNull(payloadDataCrate, "StandardPayloadData was not found in rateChooser.CrateDescriptions. Available crate descriptions are: " + string.Join("\n", crateChooser.CrateDescriptions.Select(x=> $"{x.Label} of type {x.ManifestType}")));

            payloadDataCrate.Selected = true;
            using (var updatableStorage = Crate.GetUpdatableStorage(fr8CoreLoop))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(loopConfigCrate);
            }

            fr8CoreLoop = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", fr8CoreLoop);

            //
            // Configure Send DocuSign Envelope action
            //

            // Initial Configuration
            var sendEnvelopeAction = fr8CoreLoop.ChildrenActivities.Single(a => a.Label == "Send DocuSign Envelope");

            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();

            var docuSignTemplate = controlsCrate.Content.Controls.OfType<DropDownList>().First();
            docuSignTemplate.Value = "9a4d2154-5b18-4316-9824-09432e62f458";
            docuSignTemplate.selectedKey = "Medical_Form_v1";
            docuSignTemplate.ListItems.Add(new ListItem() { Value = "9a4d2154-5b18-4316-9824-09432e62f458", Key = "Medical_Form_v1" });

            using (var updatableStorage = Crate.GetUpdatableStorage(sendEnvelopeAction))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(controlsCrate);
            }

            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", sendEnvelopeAction);
            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure", sendEnvelopeAction);


            // Follow-up Configuration
            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var emailField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingfreight testing role email");
            emailField.ValueSource = "specific";
            emailField.Value = TestEmail;
            emailField.TextValue = TestEmail;

            var emailNameField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingfreight testing role name");
            emailNameField.ValueSource = "specific";
            emailNameField.Value = TestEmailName;
            emailNameField.TextValue = TestEmailName;

            using (var updatableStorage = Crate.GetUpdatableStorage(sendEnvelopeAction))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(controlsCrate);
            }

            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", sendEnvelopeAction);

            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();

            docuSignTemplate = controlsCrate.Content.Controls.OfType<DropDownList>().First();
            Assert.AreEqual("9a4d2154-5b18-4316-9824-09432e62f458", docuSignTemplate.Value, "Selected DocuSign Template did not save on Send DocuSign Envelope action.");
            Assert.AreEqual("Medical_Form_v1", docuSignTemplate.selectedKey, "Selected DocuSign Template did not save on Send DocuSign Envelope action.");

            emailField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingfreight testing role email");
            Assert.AreEqual(TestEmail, emailField.Value, "Email did not save on Send DocuSign Envelope action.");
            Assert.AreEqual(TestEmail, emailField.TextValue, "Email did not save on Send DocuSign Envelope action.");

            emailNameField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingfreight testing role name");
            Assert.AreEqual(TestEmailName, emailNameField.Value, "Email Name did not save on Send DocuSign Envelope action.");
            Assert.AreEqual(TestEmailName, emailNameField.TextValue, "Email Name did not save on Send DocuSign Envelope action.");

            //
            // Activate and run plan
            //
            var container = await HttpPostAsync<string, ContainerDTO>(_baseUrl + "plans/run?planId=" + plan.Plan.Id, null);
            Assert.AreEqual(container.ContainerState, ContainerState.Completed);

            //
            // Deactivate plan
            //
            await HttpPostAsync<string, string>(_baseUrl + "plans/deactivate?planId=" + plan.Plan.Id, null);

            // Verify that test email has been received
            EmailAssert.EmailReceived("dse_demo@docusign.net", "Test Message from Fr8");

            //
            // Delete plan
            //
            await HttpDeleteAsync(_baseUrl + "plans?id=" + plan.Plan.Id);
        }

        [Test]
        public async Task Mail_Merge_Into_DocuSign_EndToEnd_Upstream_Values_From_Google_Check_Tabs()
        {
            //
            //Setup Test
            //
            await RevokeTokens();

            var terminalGoogleTestTools = new terminaBaselTests.Tools.Terminals.IntegrationTestTools_terminalGoogle(this);
            var googleActivityTestTools = new terminaBaselTests.Tools.Activities.IntegrationTestTools_terminalGoogle(this);
            var googleAuthTokenId = await terminalGoogleTestTools.ExtractGoogleDefaultToken();

            string spreadsheetName = Guid.NewGuid().ToString();
            string spreadsheetKeyWord = Guid.NewGuid().ToString();
            string worksheetName = "TestSheet";

            //check if excel exists with data
            string spreadsheetId = await terminalGoogleTestTools.CreateNewSpreadsheet(googleAuthTokenId, spreadsheetName, worksheetName, FixtureData.TestStandardTableData(TestEmail, spreadsheetKeyWord));

            var solutionCreateUrl = _baseUrl + "activities/create?solutionName=Mail_Merge_Into_DocuSign";

            //
            // Create solution
            //
            var plan = await HttpPostAsync<string, PlanDTO>(solutionCreateUrl, null);
            var solution = plan.Plan.SubPlans.FirstOrDefault().Activities.FirstOrDefault();

            //
            // Send configuration request without authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + solution.Id, solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            var stAuthCrate = crateStorage.CratesOfType<StandardAuthenticationCM>().FirstOrDefault();
            bool defaultDocuSignAuthTokenExists = stAuthCrate == null;

            Guid tokenGuid = Guid.Empty;
            if (!defaultDocuSignAuthTokenExists)
            {
                // Authenticate with DocuSign
                tokenGuid = await _terminalDocuSignTestTools.AuthenticateDocuSignAndAccociateTokenWithAction(solution.Id, GetDocuSignCredentials(), solution.ActivityTemplate.TerminalId);
            }

            //
            // Send configuration request with authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + solution.Id, solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            Assert.True(crateStorage.CratesOfType<StandardConfigurationControlsCM>().Any(), "Crate StandardConfigurationControlsCM is missing in API response.");

            var controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var controls = controlsCrate.Content.Controls;
            var dataSource = controls.OfType<DropDownList>().FirstOrDefault(c => c.Name == "DataSource");
            dataSource.Value = "Get_Google_Sheet_Data";
            dataSource.selectedKey = "Get Google Sheet Data";
            var template = controls.OfType<DropDownList>().FirstOrDefault(c => c.Name == "DocuSignTemplate");
            template.Value = "a439cedc-92a8-49ad-ab31-e2ee7964b468";
            template.selectedKey = "Fr8 Fromentum Registration Form";
            var button = controls.OfType<Button>().FirstOrDefault();
            button.Clicked = true;

            //
            // Configure solution
            //
            using (var crateStorage = Crate.GetUpdatableStorage(this.solution))
            {
                crateStorage.Remove<StandardConfigurationControlsCM>();
                crateStorage.Add(controlsCrate);
            }
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure?id=" + this.solution.Id, this.solution);
            crateStorage = Crate.FromDto(this.solution.CrateStorage);
            Assert.AreEqual(2, this.solution.ChildrenActivities.Count(), "Solution child actions failed to create.");

            //
            // configure Get_Google_Sheet_Data activity
            //
            var googleSheetActivity = this.solution.ChildrenActivities.Single(a=>a.Label.Equals("get google sheet data", StringComparison.InvariantCultureIgnoreCase));
            await googleActivityTestTools.ConfigureGetFromGoogleSheetActivity(googleSheetActivity, spreadsheetName, false, worksheetName);
            
            //
            // configure Loop activity
            //
            var loopActivity = this.solution.ChildrenActivities.Single(a => a.Label.Equals("loop", StringComparison.InvariantCultureIgnoreCase));
            var terminalFr8CoreTools = new IntegrationTestTools_terminalFr8(this);
            loopActivity = await terminalFr8CoreTools.ConfigureLoopActivity(loopActivity, "Standard Table Data", "Table Generated From Google Sheet Data");

            //
            // Configure Send DocuSign Envelope action
            //

            // Initial Configuration
            var sendEnvelopeAction = loopActivity.ChildrenActivities.Single(a => a.Label == "Send DocuSign Envelope");

            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();

            var docuSignTemplate = controlsCrate.Content.Controls.OfType<DropDownList>().First();
            docuSignTemplate.Value = "a439cedc-92a8-49ad-ab31-e2ee7964b468";
            docuSignTemplate.selectedKey = "Fr8 Fromentum Registration Form";
  
            using (var updatableStorage = Crate.GetUpdatableStorage(sendEnvelopeAction))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(controlsCrate);
            }

            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", sendEnvelopeAction);
            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/configure", sendEnvelopeAction);

            // Follow-up Configuration
            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var emailField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingLead role email");
            emailField.ValueSource = "upstream";
            emailField.Value = "emailaddress";
            emailField.selectedKey= "emailaddress";

            var emailNameField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingLead role name");
            emailNameField.ValueSource = "upstream";
            emailNameField.Value = "name";
            emailNameField.selectedKey = "name";

            var phoneField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "MappingPhone(Lead)");
            phoneField.ValueSource = "upstream";
            phoneField.Value = "phone";
            phoneField.selectedKey = "phone";

            var titleField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "MappingTitle(Lead)");
            titleField.ValueSource = "upstream";
            titleField.Value = "title";
            titleField.selectedKey = "title";

            var companyField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "MappingCompany(Lead)");
            companyField.ValueSource = "upstream";
            companyField.Value = "companyname";
            companyField.selectedKey = "companyname";

            var radioGroup = controlsCrate.Content.Controls.OfType<RadioButtonGroup>().First(f=>f.GroupName == "RadioGroupMappingRegistration Type(Lead)");
            var radioButton = radioGroup.Radios.FirstOrDefault(x => x.Name == "Buy 2, Get 3rd Free");
            radioButton.Selected = true;

            var checkboxField = controlsCrate.Content.Controls.OfType<CheckBox>().First(f => f.Name == "CheckBoxMappingGovernmentEntity?(Lead)");
            checkboxField.Selected = true;

            var dropdownField = controlsCrate.Content.Controls.OfType<DropDownList>().First(f => f.Name == "DropDownMappingSize of Company(Lead)");
            dropdownField.Value = "Medium (51-250)";
            dropdownField.selectedKey = "Medium (51-250)";

            using (var updatableStorage = Crate.GetUpdatableStorage(sendEnvelopeAction))
            {
                updatableStorage.Remove<StandardConfigurationControlsCM>();
                updatableStorage.Add(controlsCrate);
            }

            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(_baseUrl + "activities/save", sendEnvelopeAction);

            crateStorage = Crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();

            docuSignTemplate = controlsCrate.Content.Controls.OfType<DropDownList>().First();
            Assert.AreEqual("a439cedc-92a8-49ad-ab31-e2ee7964b468", docuSignTemplate.Value, "Selected DocuSign Template did not save on Send DocuSign Envelope action.");
            Assert.AreEqual("Fr8 Fromentum Registration Form", docuSignTemplate.selectedKey, "Selected DocuSign Template did not save on Send DocuSign Envelope action.");

            emailField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingLead role email");
            //Assert.AreEqual(TestEmail, emailField.Value, "Email did not save on Send DocuSign Envelope action.");
            //Assert.AreEqual(TestEmail, emailField.TextValue, "Email did not save on Send DocuSign Envelope action.");

            emailNameField = controlsCrate.Content.Controls.OfType<TextSource>().First(f => f.Name == "RolesMappingLead role name");
            //Assert.AreEqual(TestEmailName, emailNameField.Value, "Email Name did not save on Send DocuSign Envelope action.");
            //Assert.AreEqual(TestEmailName, emailNameField.TextValue, "Email Name did not save on Send DocuSign Envelope action.");

            //
            // Activate and run plan
            //
            var container = await HttpPostAsync<string, ContainerDTO>(_baseUrl + "plans/run?planId=" + plan.Plan.Id, null);
            Assert.AreEqual(container.ContainerState, ContainerState.Completed);

            //get the envelope from DocuSign
            var configuration =  new DocuSignManager().SetUp(_terminalDocuSignTestTools.GetDocuSignAuthToken(tokenGuid));
            //var settings = GetDocusignQuery(configurationControls);
            var folderItems = DocuSignFolders.GetFolderItems(configuration, new DocusignQuery()
            {       
                Status = "sent",
                SearchText = spreadsheetKeyWord
            });

            var envelopeId = folderItems.FirstOrDefault().EnvelopeId;

            var envelopeApi = new EnvelopesApi(configuration.Configuration);
            var recipientId = envelopeApi.ListRecipients(configuration.AccountId, envelopeId).Signers.FirstOrDefault().RecipientId;

            var tabs = envelopeApi.ListTabs(configuration.AccountId, envelopeId, recipientId);
            //

            //delete spreadsheet
            await terminalGoogleTestTools.DeleteSpreadSheet(googleAuthTokenId, spreadsheetId);
            
            //
            // Deactivate plan
            //
            await HttpPostAsync<string, string>(_baseUrl + "plans/deactivate?planId=" + plan.Plan.Id, null);

            // Verify that test email has been received
            EmailAssert.EmailReceived("dse_demo@docusign.net", "Test Message from Fr8");

            //
            // Delete plan
            //
            await HttpDeleteAsync(_baseUrl + "plans?id=" + plan.Plan.Id);
        }

        private async Task<Guid> ExtractGoogleDefaultToken()
        {
            var tokens = await HttpGetAsync<IEnumerable<ManageAuthToken_Terminal>>(
                _baseUrl + "manageauthtoken/"
            );

            Assert.NotNull(tokens);

            var terminal = tokens.FirstOrDefault(x => x.Name == "terminalGoogle");
            Assert.NotNull(terminal);

            var token = terminal.AuthTokens.FirstOrDefault(x => x.IsMain);
            Assert.NotNull(token);

            return token.Id;
        }
    }
}
