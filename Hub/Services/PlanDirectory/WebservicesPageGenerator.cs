﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Entities;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Utilities.Configuration;
using Hub.Interfaces;
using Hub.Resources;

namespace Hub.Services.PlanDirectory
{
    public class WebservicesPageGenerator : IWebservicesPageGenerator
    {
        private const string TagsSeparator = "-";
        private const string PageExtension = ".html";
        private const string PageType = "WebService";

        private readonly IPageDefinition _pageDefinitionService;
        private readonly IPlanTemplate _planTemplateService;
        private readonly ITemplateGenerator _templateGenerator;
        private readonly ITagGenerator _tagGenerator;

        public WebservicesPageGenerator(
            IPageDefinition pageDefinitionService,
            IPlanTemplate planTemplateService,
            ITemplateGenerator templateGenerator,
            ITagGenerator tagGenerator)
        {
            _pageDefinitionService = pageDefinitionService;
            _planTemplateService = planTemplateService;
            _templateGenerator = templateGenerator;
            _tagGenerator = tagGenerator;
        }

        public async Task Generate(PlanTemplateCM planTemplate, string fr8AccountId)
        {
            var storage = await _tagGenerator.GetTags(planTemplate, fr8AccountId);
            foreach (var tag in storage.WebServiceTemplateTags)
            {
                var tags = tag.TagsWithIcons.Select(x => x.Key).ToArray();
                var pageName = GeneratePageNameFromTags(tags);
                var pageDefinition = _pageDefinitionService.Get(x => x.PageName == pageName && x.Type == PageType).FirstOrDefault() ?? new PageDefinitionDO
                {
                    PageName = pageName,
                    Tags = tags,
                    Type = PageType,
                    Title = tag.Title
                };
                if (!pageDefinition.PlanTemplatesIds.Contains(planTemplate.ParentPlanId))
                {
                    pageDefinition.PlanTemplatesIds.Add(planTemplate.ParentPlanId);
                }
                pageDefinition.Url = new Uri($"{_templateGenerator.BaseUrl}/{pageName}");
                _pageDefinitionService.CreateOrUpdate(pageDefinition);
                var relatedPageDefinitions = _pageDefinitionService.GetAll().Where(x => x.PlanTemplatesIds.Contains(planTemplate.ParentPlanId));
                var curPageDefinition = relatedPageDefinitions.FirstOrDefault(x => x.PageName == pageName);
                var curRelatedPlans = new List<PublishPlanTemplateDTO>();
                foreach (var planTemplateId in curPageDefinition.PlanTemplatesIds)
                {
                    var relatedPlan = _planTemplateService.GetPlanTemplateDTO(fr8AccountId, Guid.Parse(planTemplateId)).Result;
                    if (relatedPlan != null)
                        curRelatedPlans.Add(relatedPlan);
                }
                var relatedPlans = new List<Tuple<string, string, string>>();
                foreach (var publishPlanTemplateDTO in curRelatedPlans)
                {
                    relatedPlans.Add(new
                        Tuple<string, string, string>(
                        publishPlanTemplateDTO.Name,
                        publishPlanTemplateDTO.Description ?? publishPlanTemplateDTO.Name,
                        CloudConfigurationManager.GetSetting("HubApiUrl").Replace("/api/v1/", "")
                        + "dashboard/plans/" + publishPlanTemplateDTO.ParentPlanId + "/builder?viewMode=plan"));
                }
                await _templateGenerator.Generate(new PlanCategoryTemplate(), pageName, new Dictionary<string, object>
                {
                    ["Name"] = pageName,
                    ["Tags"] = tag.TagsWithIcons,
                    ["RelatedPlans"] = relatedPlans
                });
            }
        }

        /// <summary>
        /// Generates pageName from tagsTitles
        /// </summary>
        /// <param name="tagsTitles"></param>
        /// <returns></returns>
        private static string GeneratePageNameFromTags(IEnumerable<string> tagsTitles)
        {
            return string.Join(
                TagsSeparator,
                tagsTitles.Select(x => x.ToLower()).OrderBy(x => x)) + PageExtension;
        }
    }
}