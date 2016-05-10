﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.Manifests;
using Hub.Managers;
using NUnit.Framework;
using StructureMap;
using terminalDocuSign;
using terminalDocuSign.Actions;
using terminalDocuSign.Services.New_Api;
using terminalDocuSignTests.Fixtures;
using UtilitiesTesting.Fixtures;

namespace terminalDocuSignTests.Activities
{
    [TestFixture]
    [Category("Get_DocuSign_Template_v1")]
    public class Get_DocuSign_Template_v1_Tests : BaseTest
    {
        [Test]
        public async Task ActivityIsValid_WhenIsNotConfigured_ReturnsFalse()
        {
            ObjectFactory.Configure(x => x.For<IDocuSignManager>().Use(DocuSignActivityFixtureData.DocuSignManagerWithoutTemplates()));
            var target = new Get_DocuSign_Template_v1();

            var result = await Validate(target, FixtureData.TestActivity1());

            AssertErrorMessage(result, DocuSignValidationUtils.ControlsAreNotConfiguredErrorMessage);
        }

        [Test]
        public async Task ActivityIsValid_WhenThereAreNoTemplates_ReturnsFalse()
        {
            ObjectFactory.Configure(x => x.For<IDocuSignManager>().Use(DocuSignActivityFixtureData.DocuSignManagerWithoutTemplates()));
            var target = new Get_DocuSign_Template_v1();
            var activityDO = FixtureData.TestActivity1();

            activityDO = await target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration());

            var result = await Validate(target, activityDO);

            AssertErrorMessage(result, DocuSignValidationUtils.NoTemplateExistsErrorMessage);
        }

        [Test]
        public async Task ActivityIsValid_WhenTemplateIsNotSelected_ReturnsFalse()
        {
            ObjectFactory.Configure(x => x.For<IDocuSignManager>().Use(DocuSignActivityFixtureData.DocuSignManagerWithTemplates()));
            var target = new Get_DocuSign_Template_v1();
            var activityDO = FixtureData.TestActivity1();

            activityDO = await target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration());

            var result = await Validate(target, activityDO);

            AssertErrorMessage(result, DocuSignValidationUtils.TemplateIsNotSelectedErrorMessage);
        }

        [Test]
        public async Task ActivityIsValid_WhenTemplatetIsSelected_ReturnsTrue()
        {
            ObjectFactory.Configure(x => x.For<IDocuSignManager>().Use(DocuSignActivityFixtureData.DocuSignManagerWithTemplates()));
            var target = new Get_DocuSign_Template_v1();
            var activityDO = FixtureData.TestActivity1();

            activityDO = await target.Configure(activityDO, FixtureData.AuthToken_TerminalIntegration());

            SelectTemplate(activityDO);

            var result = await Validate(target, activityDO);

            Assert.AreEqual(false, result.HasErrors);
        }

        private void SelectTemplate(ActivityDO activity)
        {
            using (var crateStorage = new CrateManager().GetUpdatableStorage(activity))
            {
                var configControls = crateStorage.FirstCrate<StandardConfigurationControlsCM>(c => true).Content;
                var templateList = configControls.Controls.OfType<DropDownList>().FirstOrDefault();
                templateList.selectedKey = "First";
            }
        }
    }
}
