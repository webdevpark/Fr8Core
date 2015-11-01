﻿using System;
using System.Web.Mvc;

namespace Hub.Managers
{
    public class DockyardAuthorizeAttribute : AuthorizeAttribute
    {
        public DockyardAuthorizeAttribute(params string[] roles)
        {
            Roles = String.Join(",", roles);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext context)
        {
            // redirect to Error page
            context.Result = new RedirectResult("/DockyardAccount/InterceptLogin?returnUrl=" + context.RequestContext.HttpContext.Request.RawUrl + "&urlReferrer=" + context.RequestContext.HttpContext.Request.UrlReferrer);
        }
    }
}
