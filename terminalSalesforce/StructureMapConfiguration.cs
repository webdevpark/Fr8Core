﻿using System;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Moq;
using Owin;
using StructureMap;
using StructureMap.Configuration.DSL;
using Data.Infrastructure;
using Data.Interfaces;
using Hub.Interfaces;
using Hub.Services;
using Hub.StructureMap;
using Utilities;
using terminalSalesforce.Infrastructure;
using terminalSalesforce.Services;



namespace terminalSalesforce
{
    public class TerminalSalesforceStructureMapBootstrapper
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
                     ObjectFactory.Initialize(x => x.AddRegistry<TestMode>()); 
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
                 For<terminalSalesforce.Infrastructure.IEvent>().Use<terminalSalesforce.Services.Event>();
                 For<ISalesforceManager>().Use<terminalSalesforce.Services.SalesforceManager>();                  
             }
         }       

         public class TestMode : StructureMapBootStrapper.TestMode
         {
             public TestMode()
             {
                 For<terminalSalesforce.Infrastructure.IEvent>().Use<terminalSalesforce.Services.Event>();

                 Mock<ISalesforceManager> salesforceIntegrationMock = new Mock<ISalesforceManager>(MockBehavior.Default);
                 For<ISalesforceManager>().Use(salesforceIntegrationMock.Object);
             }
         }       

        public static void LiveConfiguration(ConfigurationExpression configuration)
        {
            configuration.AddRegistry<LiveMode>();
        }
    }
}