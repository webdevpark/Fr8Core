﻿using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Hub.Managers;
using Data.Interfaces.Manifests;
using Data.Control;

namespace terminalFr8Core.Actions
{
    public class Build_Message_v1 : BaseTerminalAction
    {
        public override async Task<ActionDO> Configure(ActionDO curActionDataPackageDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActionDataPackageDO, ConfigurationEvaluator, authTokenDO);
        }

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var curProcessPayload = await GetProcessPayload(curActionDO, containerId);

            var controlsMS = Crate.GetStorage(curActionDO.CrateStorage).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (controlsMS == null)
            {
                throw new ApplicationException("Could not find ControlsConfiguration crate.");
            }

            var fieldListControl = controlsMS.Controls
                .SingleOrDefault(x => x.Type == ControlTypes.FieldList);

            if (fieldListControl == null)
            {
                throw new ApplicationException("Could not find FieldListControl.");
            }

            var userDefinedPayload = JsonConvert.DeserializeObject<List<FieldDTO>>(fieldListControl.Value);

            //using (var updater = Crate.UpdateStorage(() => processPayload.CrateStorage))
            //{
            //    updater.CrateStorage.Add(Data.Crates.Crate.FromContent("ManuallyAddedPayload", new StandardPayloadDataCM(userDefinedPayload)));
            //}

            return curProcessPayload;

        }



        protected async override Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            //build a controls crate to render the pane
            var textBlock = new TextBlock()
            {
                Label = "Create a Message",
                Name = "CreateaMessage"
            };
            var name = new TextBox()
            {
                Label = "Name:",
                Name = "Name",
                Events = new List<ControlEvent>() { new ControlEvent("onChange", "requestConfig") }

            };

            var messageBody = new TextArea()
            {
                Label = "Body:",
                Name = "Body",
                Events = new List<ControlEvent>() { new ControlEvent("onChange", "requestConfig") }
            };
            var curMergedUpstreamRunTimeObjects = await MergeUpstreamFields(curActionDO, "Available Run-Time Objects");
            //added focusConfig in PaneConfigureAction.ts
            var fieldSelectObjectTypes = new DropDownList()
            {
                Label = "Available Fields",
                Name = "Available Fields",
                Required = true,
                Events = new List<ControlEvent>() { new ControlEvent("onChange", "focusConfig") },
                Source = new FieldSourceDTO
                {
                    Label = curMergedUpstreamRunTimeObjects.Label,
                    ManifestType = curMergedUpstreamRunTimeObjects.ManifestType.Type
                }
            };

            var curConfigurationControlsCrate = PackControlsCrate(textBlock, name, messageBody, fieldSelectObjectTypes);


            FieldDTO[] curSelectedFields = curMergedUpstreamRunTimeObjects.Content.Fields.Select(field => new FieldDTO { Key = field.Key, Value = field.Value }).ToArray();

            var curSelectedObjectType = Crate.CreateDesignTimeFieldsCrate("SelectedObjectTypes", curSelectedFields);

            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                if (curActionDO.CrateStorage == null)
                {
                    updater.CrateStorage.Clear();
                    updater.CrateStorage.Add(curConfigurationControlsCrate);
                    updater.CrateStorage.Add(curMergedUpstreamRunTimeObjects);
                    updater.CrateStorage.Add(curSelectedObjectType);
                }
            }

            return curActionDO;
        }


        private ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            return ConfigurationRequestType.Initial;
        }
    }
}