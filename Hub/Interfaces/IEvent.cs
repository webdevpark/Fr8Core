﻿using System.Threading.Tasks;
using Data.Entities;
using System.Collections.Generic;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;

namespace Hub.Interfaces
{
    /// <summary>
    /// Event service interface
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Handles Terminal Incident
        /// </summary>
        void HandleTerminalIncident(LoggingDataCm incident);
        
        /// <summary>
        /// Handles Terminal Event 
        /// </summary>
        void HandleTerminalEvent(LoggingDataCm eventDataCm);

        Task ProcessInboundEvents(Crate curCrateStandardEventReport);
        Task LaunchProcess(PlanDO curPlan, Crate curEventData = null);
        Task LaunchProcesses(List<PlanDO> curPlans, Crate curEventReport);
    }
}
