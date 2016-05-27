﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Entities;
using Fr8Data.Constants;
using Fr8Data.Control;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using Hub.Managers;
using terminalDocuSign.Actions;
using TerminalBase.Infrastructure;
using TerminalBase.Models;
using Utilities;

namespace terminalDocuSign.Activities
{
    public class Archive_DocuSign_Template_v1 : BaseDocuSignActivity
    {
        protected override ActivityTemplateDTO MyTemplate { get; }


        private const string SolutionName = "Archive DocuSign Template";
        private const double SolutionVersion = 1.0;
        private const string TerminalName = "DocuSign";
        private const string SolutionBody = @"<p>This is Archive DocuSign Template solution action</p>";

        private class ActivityUi : StandardConfigurationControlsCM
        {
            public ActivityUi()
            {
                Controls = new List<ControlDefinitionDTO>();
                Controls.Add(new DropDownList
                {
                    Label = "Archive which template",
                    Name = "Available_Templates",
                    Value = null,
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                });

                Controls.Add(new TextBox
                {
                    Label = "Destination File Name",
                    Name = "File_Name",
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                });
            }
        }
        

        protected override Task InitializeDS()
        {
            var configurationCrate = PackControls(new ActivityUi());
            FillDocuSignTemplateSource(configurationCrate, "Available_Templates");
            Storage.Clear();
            Storage.Add(configurationCrate);
            return Task.FromResult(0);
        }

        protected override async Task FollowUpDS()
        {
            var selectedTemplateField = GetControl<DropDownList>("Available_Templates", ControlTypes.DropDownList);
            if (string.IsNullOrEmpty(selectedTemplateField.Value))
            {
                return;
            }

            var destinationFileNameField = GetControl<TextBox>("File_Name", ControlTypes.TextBox);
            if (string.IsNullOrEmpty(destinationFileNameField.Value))
            {
                return;
            }

            var getDocusignTemplate = await GetActivityTemplate("terminalDocusign", "Get_DocuSign_Template");
            var convertCratesTemplate = await GetActivityTemplate("terminalFr8Core", "ConvertCrates");
            var storeFileTemplate = await GetActivityTemplate("terminalFr8Core", "StoreFile");

            var getDocuSignTemplateActivity = await CreateGetDocuSignTemplateActivity(getDocusignTemplate, ActivityPayload);
            var convertCratesActivity = await CreateConvertCratesActivity(convertCratesTemplate, ActivityPayload);
            var storeFileActivity = await CreateStoreFileActivity(storeFileTemplate, ActivityPayload);

            SetSelectedTemplate(getDocuSignTemplateActivity, selectedTemplateField);
            SetFromConversion(convertCratesActivity);
            convertCratesActivity = await HubCommunicator.ConfigureActivity(convertCratesActivity, CurrentUserId);
            SetToConversion(convertCratesActivity);
            SetFileDetails(storeFileActivity, destinationFileNameField.Value);
            //add child nodes here
            ActivityPayload.ChildrenActivities.Add(getDocuSignTemplateActivity);
            ActivityPayload.ChildrenActivities.Add(convertCratesActivity);
            ActivityPayload.ChildrenActivities.Add(storeFileActivity);
        }

        private async Task<ActivityPayload> CreateGetDocuSignTemplateActivity(ActivityTemplateDTO template, ActivityPayload parentAction)
        {
            var authTokenId = Guid.Parse(AuthorizationToken.Id);
            return await HubCommunicator.CreateAndConfigureActivity(template.Id, CurrentUserId, "Get Docusign Template", 1, parentAction.Id, false, authTokenId);
        }
        private async Task<ActivityPayload> CreateConvertCratesActivity(ActivityTemplateDTO template, ActivityPayload parentAction)
        {
            return await HubCommunicator.CreateAndConfigureActivity(template.Id, CurrentUserId, "Convert Crates", 2, parentAction.Id);
        }
        private async Task<ActivityPayload> CreateStoreFileActivity(ActivityTemplateDTO template, ActivityPayload parentAction)
        {
            return await HubCommunicator.CreateAndConfigureActivity(template.Id, CurrentUserId, "Store File", 3, parentAction.Id);
        }

        private void SetFileDetails(ActivityPayload storeFileActivity, string fileName)
        {
            var confControls = ControlHelper.GetConfigurationControls(storeFileActivity.CrateStorage);
            var fileNameTextbox = ControlHelper.GetControl<TextBox>(confControls, "File_Name", ControlTypes.TextBox);
            var fileCrateTextSource = ControlHelper.GetControl<TextSource>(confControls, "File Crate label", ControlTypes.TextSource);
            fileNameTextbox.Value = fileName;
            fileCrateTextSource.ValueSource = "specific";
            fileCrateTextSource.TextValue = "From DocuSignTemplate To StandardFileDescription";
        }

        private void SetFromConversion(ActivityPayload convertCratesActivity)
        {
            var confControls = ControlHelper.GetConfigurationControls(convertCratesActivity.CrateStorage);
            var fromDropdown = ControlHelper.GetControl<DropDownList>(confControls, "Available_From_Manifests", ControlTypes.DropDownList);
            fromDropdown.Value = ((int)MT.DocuSignTemplate).ToString(CultureInfo.InvariantCulture);
            fromDropdown.selectedKey = MT.DocuSignTemplate.GetEnumDisplayName();
        }

        private void SetToConversion(ActivityPayload convertCratesActivity)
        {
            var confControls = ControlHelper.GetConfigurationControls(convertCratesActivity.CrateStorage);
            var toDropdown = ControlHelper.GetControl<DropDownList>(confControls, "Available_To_Manifests", ControlTypes.DropDownList);
            toDropdown.Value = ((int)MT.StandardFileHandle).ToString(CultureInfo.InvariantCulture);
            toDropdown.selectedKey = MT.StandardFileHandle.GetEnumDisplayName();
        }

        private void SetSelectedTemplate(ActivityPayload docuSignActivity, DropDownList selectedTemplateDd)
        {
            var confControls = ControlHelper.GetConfigurationControls(docuSignActivity.CrateStorage);
            var actionDdlb = ControlHelper.GetControl<DropDownList>(confControls, "Available_Templates", ControlTypes.DropDownList);
            actionDdlb.selectedKey = selectedTemplateDd.selectedKey;
            actionDdlb.Value = selectedTemplateDd.Value;
        }

        protected override string ActivityUserFriendlyName => SolutionName;

        protected override async Task RunDS()
        {
            Success();
            await Task.Yield();
        }

        /// <summary>
        /// This method provides documentation in two forms:
        /// SolutionPageDTO for general information and 
        /// ActivityResponseDTO for specific Help on minicon
        /// </summary>
        /// <param name="activityPayload"></param>
        /// <param name="curDocumentation"></param>
        /// <returns></returns>
        public dynamic Documentation(ActivityPayload activityPayload, string curDocumentation)
        {
            if (curDocumentation.Contains("MainPage"))
            {
                var curSolutionPage = GetDefaultDocumentation(SolutionName, SolutionVersion, TerminalName, SolutionBody);
                return Task.FromResult(curSolutionPage);
            }
            if (curDocumentation.Contains("HelpMenu"))
            {
                if (curDocumentation.Contains("ExplainArchiveTemplate"))
                {
                    return Task.FromResult(GenerateDocumentationResponse(@"This solution work with DocuSign templates"));
                }
                if (curDocumentation.Contains("ExplainService"))
                {
                    return Task.FromResult(GenerateDocumentationResponse(@"This solution works and DocuSign service and uses Fr8 infrastructure"));
                }
                return Task.FromResult(GenerateErrorResponse("Unknown contentPath"));
            }
            return
                Task.FromResult(
                    GenerateErrorResponse("Unknown displayMechanism: we currently support MainPage and HelpMenu cases"));
        }

        
    }
}