using Microsoft.AspNetCore.Mvc.Filters;

namespace MyLaboratory.Site.Filters
{
    public class GlobalActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Do something before the action executes.
            //        ...
            //if (needToRedirect)
            //        {
            //            ...
            //   filterContext.Result = new RedirectResult(url);
            //            return;
            //        }
            //        ...

            //var controllerName = routeData.Values["controller"];
            //var actionName = routeData.Values["action"];
            //var message = String.Format("{0} controller:{1} action:{2}", methodName, controllerName, actionName);
            //Debug.WriteLine(message, "Action Filter Log");

            //string actionName = context.ActionDescriptor.ac;
            //string controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName
            //base.OnActionExecuting(filterContext);
            var controllerName = context.ActionDescriptor.RouteValues["controller"].ToString();
            var actionName = context.ActionDescriptor.RouteValues["action"].ToString();

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something after the action executes.

        }
    }
}