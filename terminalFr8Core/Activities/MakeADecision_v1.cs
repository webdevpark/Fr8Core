﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fr8Data.Constants;
using Fr8Data.Control;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Managers;
using Fr8Data.Manifests;
using Fr8Data.States;
using TerminalBase.Infrastructure;

namespace terminalFr8Core.Activities
{
    public class MakeADecision_v1 : TestIncomingData_v1
    {
        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Name = "MakeADecision",
            Label = "Make a Decision",
            Version = "1",
            Category = ActivityCategory.Processors,
            NeedsAuthentication = false,
            MinPaneWidth = 550,
            WebService = TerminalData.WebServiceDTO,
            Terminal = TerminalData.TerminalDTO
        };
        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;

#if DEBUG
        private const int SmoothRunLimit = 5;
        private const int SlowRunLimit = 10;
#else
        private const int SmoothRunLimit = 100;
        private const int SlowRunLimit = 250;
#endif

        private const int MinAllowedElapsedTimeInSeconds = 12;


        public MakeADecision_v1(ICrateManager crateManager) 
            : base(crateManager)
        {
        }

        public override async Task RunTests()
        {
            //let's check current branch status
            var currentBranch = OperationalState.CallStack.GetLocalData<OperationalStateCM.BranchStatus>("Branch") ??
                                CreateBranch();
                currentBranch.Count += 1;
                if (currentBranch.Count >= SlowRunLimit)
                {
                RaiseError("This container hit a maximum loop count and was stopped because we're afraid it might be an infinite loop");
                return;
                }
                if (currentBranch.Count >= SmoothRunLimit)
                {
                    //it seems we need to slow down things
                    var diff = DateTime.UtcNow - currentBranch.LastBranchTime;
                    if (diff.TotalSeconds < MinAllowedElapsedTimeInSeconds)
                    {
                        await Task.Delay(10000);
                    }
                }

                currentBranch.LastBranchTime = DateTime.UtcNow;
            OperationalState.CallStack.StoreLocalData("Branch", currentBranch);
                
            var payloadFields = GetAllPayloadFields().Where(f => !string.IsNullOrEmpty(f.Key) && !string.IsNullOrEmpty(f.Value)).AsQueryable();
            var containerTransition = (ContainerTransition)ConfigurationControls.Controls.Single();
            foreach (var containerTransitionField in containerTransition.Transitions)
            {
                if (CheckConditions(containerTransitionField.Conditions, payloadFields))
                {
                    //let's return whatever this one says
                    switch (containerTransitionField.Transition)
                    {
                        case ContainerTransitions.JumpToActivity:
                            if (!containerTransitionField.TargetNodeId.HasValue)
                            {
                                RaiseError("Target Activity for transition is not specified. Please choose it in the Make a Decision activity settings and re-run the Plan.", ActivityErrorCode.DESIGN_TIME_DATA_MISSING);
                                return;
                            }
                            RequestJumpToActivity(containerTransitionField.TargetNodeId.Value);
                            return;
                        case ContainerTransitions.LaunchAdditionalPlan:
                            if (!containerTransitionField.TargetNodeId.HasValue)
                            {
                                RaiseError("Target Additional Plan for transition is not specified. Please choose it in the Make a Decision activity settings and re-run the Plan.", ActivityErrorCode.DESIGN_TIME_DATA_MISSING);
                                return;
                            }
                            LaunchPlan(containerTransitionField.TargetNodeId.Value);
                            return;
                        case ContainerTransitions.JumpToSubplan:
                            if (!containerTransitionField.TargetNodeId.HasValue)
                            {
                                RaiseError("Target Subplan for transition is not specified. Please choose it in the Make a Decision activity settings and re-run the Plan.", ActivityErrorCode.DESIGN_TIME_DATA_MISSING);
                                return;
                            }
                            RequestJumpToSubplan(containerTransitionField.TargetNodeId.Value);
                            return;
                        case ContainerTransitions.ProceedToNextActivity:
                            Success();
                            return;
                        case ContainerTransitions.StopProcessing:
                            TerminateHubExecution();
                            return;
                        case ContainerTransitions.SuspendProcessing:
                            throw new NotImplementedException();

                        default:
                            RaiseError("Invalid data was selected on MakeADecision_v1#Run", ActivityErrorCode.DESIGN_TIME_DATA_MISSING);
                            return;
                    }
                }
            }
            //none of them matched let's continue normal execution
            Success();
        }

        private OperationalStateCM.BranchStatus CreateBranch()
        {
            return new OperationalStateCM.BranchStatus
            {
                Count = 0,
                LastBranchTime = DateTime.UtcNow
            };
        }

        private bool CheckConditions(List<FilterConditionDTO> conditions, IQueryable<FieldDTO> fields)
        {
            var filterExpression = ParseCriteriaExpression(conditions, fields);
            foreach (FieldDTO field in fields)
            {
                double result = 0D;
                if (Double.TryParse(field.Value, 
                    System.Globalization.NumberStyles.AllowCurrencySymbol 
                    | System.Globalization.NumberStyles.AllowDecimalPoint 
                    | System.Globalization.NumberStyles.AllowThousands, 
                    System.Globalization.CultureInfo.CurrentCulture,
                    out result))
                {
                    field.Value = result.ToString();
                }
            }

            var results = fields.Provider.CreateQuery<FieldDTO>(filterExpression);
            return results.Any();

        }

        protected override Crate CreateControlsCrate()
        {
            var transition = new ContainerTransition
            {
                Label = "Please enter transition",
                Name = "transition",
                Source = new FieldSourceDTO()
                {
                    ManifestType = CrateManifestTypes.StandardDesignTimeFields,
                    RequestUpstream = true
                }
            };


            return PackControlsCrate(transition);
        }
    }
}