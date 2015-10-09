using PodioAspNetSample.Controllers;
using PodioAspNetSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PodioAspNetSample.Filters
{
    public class PodioAuthenticationFilter: ActionFilterAttribute, IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (PodioConnection.IsAuthenticated)
            {
                ((LeadsController)filterContext.Controller).PodioClient = PodioConnection.GetClient();
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { action = "Index", controller = "Authorization" }));
            }
            this.OnActionExecuting(filterContext);
        }
    }
}