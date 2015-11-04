﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using StructureMap;
using Data.Entities;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Services;

namespace HubWeb.Controllers
{
    [RoutePrefix("actions")]
    public class ActionController : ApiController
    {
        private readonly IAction _action;
        private readonly ISecurityServices _security;
        private readonly IActivityTemplate _activityTemplate;
        private readonly ISubroute _subRoute;
        private readonly IRoute _route;

        private readonly Authorization _authorization;

        public ActionController()
        {
            _action = ObjectFactory.GetInstance<IAction>();
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _subRoute = ObjectFactory.GetInstance<ISubroute>();
            _route = ObjectFactory.GetInstance<IRoute>();
            _authorization = new Authorization();
        }

        public ActionController(IAction service)
        {
            _action = service;
        }

        public ActionController(ISubroute service)
        {
            _subRoute = service;
        }



        [HttpPost]
        //[Fr8ApiAuthorize]
        [Route("create")]
        public async Task<IHttpActionResult> Create(int actionTemplateId, string name, string label = null, int? parentNodeId = null, bool createRoute = false)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var userId = User.Identity.GetUserId();

                var result = await _action.CreateAndConfigure(uow, userId, actionTemplateId, name, label, parentNodeId, createRoute);

                if (result is ActionDO)
                {
                    return Ok(Mapper.Map<ActionDTO>(result));
                }

                if (result is RouteDO)
                {
                    return Ok(_route.MapRouteToDto(uow, (RouteDO)result));
                }

                throw new Exception("Unsupported type " + result.GetType());
            }
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        [Route("create")]
        public async Task<IHttpActionResult> Create(string solutionName)
        {
            var userId = User.Identity.GetUserId();

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityTemplate = uow.ActivityTemplateRepository.GetAll().
                    Where(at => at.Name == solutionName).FirstOrDefault();
                if (activityTemplate == null)
                {
                    throw new ArgumentException(String.Format("actionTemplate (solution) name {0} is not found in the database.", solutionName));
                }

                var result = await _action.CreateAndConfigure(uow, userId,
                    activityTemplate.Id, activityTemplate.Name, activityTemplate.Label, null, true);
                return Ok(_route.MapRouteToDto(uow, (RouteDO)result));
            }
        }


        //WARNING. there's lots of potential for confusion between this POST method and the GET method following it.

        [HttpPost]
        [Route("configuration")]
        [Route("configure")]
        //[ResponseType(typeof(CrateStorageDTO))]
        public async Task<IHttpActionResult> Configure(ActionDTO curActionDesignDTO)
        {
            curActionDesignDTO.CurrentView = null;
            ActionDO curActionDO = Mapper.Map<ActionDO>(curActionDesignDTO);
            ActionDTO actionDTO = await _action.Configure(User.Identity.GetUserId(), curActionDO);
            return Ok(actionDTO);
        }

        /// <summary>
        /// GET : Returns an action with the specified id
        /// </summary>
        [HttpGet]
        [Route("{id:int}")]
        public ActionDTO Get(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return Mapper.Map<ActionDTO>(_action.GetById(uow, id));
            }
        }

        /// <summary>
        /// GET : Returns an action with the specified id
        /// </summary>
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id, bool confirmed = false)
        {
            var msg = await _subRoute.DeleteAction(id, confirmed);
            return Ok(msg);
        }

        /// <summary>
        /// POST : Saves or updates the given action
        /// </summary>
        [HttpPost]
        [Route("save")]
        public IHttpActionResult Save(ActionDTO curActionDTO)
        {
            ActionDO submittedActionDO = Mapper.Map<ActionDO>(curActionDTO);

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var resultActionDO = _action.SaveOrUpdateAction(uow, submittedActionDO);
                var resultActionDTO = Mapper.Map<ActionDTO>(resultActionDO);

                return Ok(resultActionDTO);
            }
        }

//        /// <summary>
//        /// POST : updates the given action
//        /// </summary>
//        [HttpPost]
//        [Route("update")]
//        public IHttpActionResult Update(ActionDTO curActionDTO)
//        {
//            ActionDO submittedActionDO = Mapper.Map<ActionDO>(curActionDTO);
//
//            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
//            {
//                await _action.SaveUpdateAndConfigure(uow, submittedActionDO);
//            }
//
//            return Ok();
//        }    
            }
}