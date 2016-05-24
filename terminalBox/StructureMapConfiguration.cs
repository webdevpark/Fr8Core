﻿using StructureMap;
using Hub.Interfaces;
using Hub.Services;
using Hub.StructureMap;
using Hub.Managers;

namespace terminalBox
{
    public class BoxStructureMapBootstrapper
    {
        public enum DependencyType
        {
            TEST = 0,
            LIVE = 1
        }
        public static void ConfigureDependencies(DependencyType type)
        {
            switch (type)
            {
                case DependencyType.TEST:
                    ObjectFactory.Initialize(x => x.AddRegistry<LiveMode>()); // No test mode yet
                    break;
                case DependencyType.LIVE:
                    ObjectFactory.Initialize(x => x.AddRegistry<LiveMode>());
                    break;
            }
        }
        public class LiveMode : StructureMapBootStrapper.LiveMode
        {
            public LiveMode()
            {
                For<IActivity>().Use<Hub.Services.Activity>();
                For<ITerminal>().Use<Terminal>().Singleton();
                For<ICrateManager>().Use<CrateManager>();
                For<IPlanNode>().Use<PlanNode>();
            }
        }

        public static void LiveConfiguration(ConfigurationExpression configuration)
        {
            configuration.AddRegistry<LiveMode>();
        }

    }
}