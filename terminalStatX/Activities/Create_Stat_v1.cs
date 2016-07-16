﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Fr8.Infrastructure.Data.Control;
using Fr8.Infrastructure.Data.Crates;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Managers;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Data.States;
using Fr8.TerminalBase.BaseClasses;
using Fr8.TerminalBase.Infrastructure;
using Newtonsoft.Json;
using terminalStatX.DataTransferObjects;
using terminalStatX.Helpers;
using terminalStatX.Interfaces;

namespace terminalStatX.Activities
{
    public class Create_Stat_v1 : TerminalActivity<Create_Stat_v1.ActivityUi>
    {
        private readonly IStatXIntegration _statXIntegration;

        public Create_Stat_v1(ICrateManager crateManager, IStatXIntegration statXIntegration) : base(crateManager)
        {
            _statXIntegration = statXIntegration;
        }

        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Name = "Create_Stat",
            Label = "Create Stat",
            Version = "1",
            Category = ActivityCategory.Forwarders,
            Terminal = TerminalData.TerminalDTO,
            NeedsAuthentication = true,
            MinPaneWidth = 300,
            WebService = TerminalData.WebServiceDTO,
            Categories = new[]
            {
                ActivityCategories.Forward,
                new ActivityCategoryDTO(TerminalData.WebServiceDTO.Name, TerminalData.WebServiceDTO.IconPath)
            }
        };

        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;

        private string SelectedStatType
        {
            get { return this[nameof(SelectedStatType)]; }
            set { this[nameof(SelectedStatType)] = value; }
        }

        public class ActivityUi : StandardConfigurationControlsCM
        {
            public DropDownList StatTypesList { get; set; }

            public RadioButtonOption UseNewStatXGroupOption { get; set; }

            public TextBox NewStatXGroupName { get; set; }

            public RadioButtonOption UseExistingStatXGroupOption { get; set; }

            public DropDownList ExistingStatGroupList { get; set; }

            public RadioButtonGroup StatXGroupsSelectionGroup { get; set; }

            [DynamicControls]
            public List<TextSource> AvailableStatProperties { get; set; }

            [DynamicControls]
            public List<FieldList> AvailableStatItemsList { get; set; }

            public ActivityUi()
            {
                StatTypesList = new DropDownList
                {
                    Label = "Choose a Stat Type",
                    Name = nameof(StatTypesList),
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                };

                NewStatXGroupName = new TextBox
                {
                    Value = "New StatX Group",
                    Name = nameof(NewStatXGroupName)
                };

                ExistingStatGroupList = new DropDownList
                {
                    Name = nameof(ExistingStatGroupList),
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                };

                UseNewStatXGroupOption = new RadioButtonOption
                {
                    Selected = true,
                    Name = nameof(UseNewStatXGroupOption),
                    Value = "Create Stat in a new Group",
                    Controls = new List<ControlDefinitionDTO> { NewStatXGroupName }
                };

                UseExistingStatXGroupOption = new RadioButtonOption()
                {
                    Selected = false,
                    Name = nameof(UseExistingStatXGroupOption),
                    Value = "Create Stat in an existing Group",
                    Controls = new List<ControlDefinitionDTO> { ExistingStatGroupList }
                };

                StatXGroupsSelectionGroup = new RadioButtonGroup
                {
                    GroupName = nameof(StatXGroupsSelectionGroup),
                    Name = nameof(StatXGroupsSelectionGroup),
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig },
                    Radios = new List<RadioButtonOption>
                            {
                                UseNewStatXGroupOption,
                                UseExistingStatXGroupOption
                            }
                };

                AvailableStatProperties = new List<TextSource>();
                AvailableStatItemsList = new List<FieldList>();
                Controls = new List<ControlDefinitionDTO>() { StatTypesList, StatXGroupsSelectionGroup };
            }

            public void ClearDynamicFields()
            {
                AvailableStatProperties?.Clear();
                AvailableStatItemsList?.Clear();
            }
        }
        public override async Task Initialize()
        {
            ActivityUI.StatTypesList.ListItems = StatXUtilities.StatTypesDictionary.Select(x => new ListItem() {Key = x.Value, Value = x.Key}).ToList();
            ActivityUI.ExistingStatGroupList.ListItems =  (await _statXIntegration.GetGroups(StatXUtilities.GetStatXAuthToken(AuthorizationToken))).Select(x => new ListItem {Key = x.Name, Value = x.Id}).ToList();
        }

        public override async Task FollowUp()
        {
            if (!string.IsNullOrEmpty(ActivityUI.StatTypesList.Value))
            {
                var previousGroup = SelectedStatType;
                if (string.IsNullOrEmpty(previousGroup) || !string.Equals(previousGroup, ActivityUI.StatTypesList.Value))
                {
                    var propertyInfos = StatXUtilities.GetStatTypeProperties(ActivityUI.StatTypesList.Value);
                    ActivityUI.ClearDynamicFields();
                    foreach (var property in propertyInfos)
                    {
                        var t = property.PropertyType;
                        if (t.IsGenericType && typeof(IList<>).IsAssignableFrom(t.GetGenericTypeDefinition()) ||
                            t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>)))
                        {
                            //render corresponding FieldList control for properties that are collections
                            var fieldlistPane = new FieldList
                            {
                                Label = "Add Multiple Stat Items",
                                Name = "Selected_Fields",
                                Required = true,
                                Events = new List<ControlEvent>() { ControlEvent.RequestConfig }
                            };
                            ActivityUI.AvailableStatItemsList.Add( fieldlistPane);
                        }
                        else
                        {
                            ActivityUI.AvailableStatProperties.Add(UiBuilder.CreateSpecificOrUpstreamValueChooser(property.Name, property.Name, requestUpstream: true, groupLabelText: "Available Stat Properties"));
                        }
                    }
                }
                SelectedStatType = ActivityUI.StatTypesList.Value;
            }
            else
            {
                ActivityUI.ClearDynamicFields();
                SelectedStatType = string.Empty;
            }
        }

        public async override Task Run()
        {
            string groupId = ActivityUI.ExistingStatGroupList.Value;
            if (ActivityUI.UseNewStatXGroupOption.Selected)
            {
                //create at first new StatX group
                var statGroup = await _statXIntegration.CreateGroup(StatXUtilities.GetStatXAuthToken(AuthorizationToken), ActivityUI.NewStatXGroupName.Value);
                groupId = statGroup.Id;
            }

            var statProperties = new List<KeyValueDTO>();
            if (ActivityUI.AvailableStatProperties != null && ActivityUI.AvailableStatProperties.Any())
            {
                statProperties.AddRange(ActivityUI.AvailableStatProperties.Select(x => new KeyValueDTO() { Key = x.Name, Value = x.GetValue(Payload) }).ToList());
            }

            var statItemsList = new List<KeyValueDTO>();
            if (ActivityUI.AvailableStatItemsList != null && ActivityUI.AvailableStatItemsList.Any())
            {
                statItemsList.AddRange(JsonConvert.DeserializeObject<List<KeyValueDTO>>(ActivityUI.AvailableStatItemsList.FirstOrDefault().Value));
            }

            var statDTO = StatXUtilities.CreateStatFromDynamicStatProperties(ActivityUI.StatTypesList.Value, statProperties, statItemsList);
            await _statXIntegration.CreateStat(StatXUtilities.GetStatXAuthToken(AuthorizationToken), groupId, statDTO);
            
            Success();
        }
    }
}