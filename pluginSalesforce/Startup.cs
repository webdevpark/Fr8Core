﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;
using terminal_base;
using terminal_base.BaseClasses;
using pluginSalesforce;

[assembly: OwinStartup(typeof(pluginSalesforce.Startup))]

namespace pluginSalesforce
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {            
            PluginSalesforceStructureMapBootstrapper.ConfigureDependencies(PluginSalesforceStructureMapBootstrapper.DependencyType.LIVE);

            Task.Run(() =>
            {
                BaseTerminalController curController = new BaseTerminalController();
                curController.AfterStartup("plugin_salesforce");
            });
        }
    }
}
