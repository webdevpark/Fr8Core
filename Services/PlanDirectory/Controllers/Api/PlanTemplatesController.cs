﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using StructureMap;
using Hub.Infrastructure;
using Microsoft.AspNet.Identity;
using PlanDirectory.Infrastructure;
using PlanDirectory.Interfaces;

namespace PlanDirectory.Controllers
{
    public class PlanTemplatesController : ApiController
    {
        private readonly IPlanTemplate _planTemplate;


        public PlanTemplatesController()
        {
            _planTemplate = ObjectFactory.GetInstance<IPlanTemplate>();
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        [PlanDirectoryHMACAuthenticate]
        public async Task<IHttpActionResult> Post(PublishPlanTemplateDTO dto)
        {
            var fr8AccountId = User.Identity.GetUserId();
            await _planTemplate.CreateOrUpdate(fr8AccountId, dto);

            return Ok();
        }

        [HttpGet]
        [Fr8ApiAuthorize]
        [PlanDirectoryHMACAuthenticate]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            var fr8AccountId = User.Identity.GetUserId();
            await _planTemplate.Get(fr8AccountId, id);

            return Ok();
        }

        // TODO: FR-3539: remove this.
        // [HttpPost]
        // [Fr8ApiAuthorize]
        // [PlanDirectoryHMACAuthenticate]
        // public async Task<IHttpActionResult> Publish(PublishPlanTemplateDTO planTemplate)
        // {
        //     await _planTemplate.Publish(planTemplate);
        //     return Ok();
        // }
        // 
        // [HttpPost]
        // [Fr8ApiAuthorize]
        // [PlanDirectoryHMACAuthenticate]
        // public async Task<IHttpActionResult> Unpublish(PublishPlanTemplateDTO planTemplate)
        // {
        //     await _planTemplate.Unpublish(planTemplate);
        //     return Ok();
        // }
        // 
        // [HttpPost]
        // public async Task<IHttpActionResult> Search(
        //     string text, int? pageStart = null, int? pageSize = null)
        // {
        //     var searchRequest = new SearchRequestDTO()
        //     {
        //         Text = text,
        //         PageStart = pageStart.GetValueOrDefault(),
        //         PageSize = pageSize.GetValueOrDefault()
        //     };
        // 
        //     var searchResult = await _planTemplate.Search(searchRequest);
        //     return Ok(searchResult);
        // }
        // 
        // [HttpPost]
        // public IHttpActionResult CreatePlan(CreatePlanDTO dto)
        // {
        //     return Ok();
        // }
    }
}