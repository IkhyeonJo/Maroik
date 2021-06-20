using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyLaboratory.Site.Filters
{
    public class GlobalActionFilter : ActionFilterAttribute
    {
        // Do something before the action executes.
        public override void OnActionExecuting(ActionExecutingContext context)
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


            #region Valid Code
            //var controllerName = context.ActionDescriptor.RouteValues["controller"].ToString();
            //var actionName = context.ActionDescriptor.RouteValues["action"].ToString();

            //if (true)
            //{
            //    //context.Result = new EmptyResult();
            //    context.Result = new UnauthorizedObjectResult("user is unauthorized"); // Action Cancel !! https://stackoverflow.com/questions/5186425/best-way-to-abort-cancel-action-and-response-from-actionfilter

            //}
            //else
            //{
            //    base.OnActionExecuting(context); // Do Action!!
            //}
            #endregion
        }

        // Do something after the action executes.
        public override void OnActionExecuted(ActionExecutedContext context)
        {   
            // Do something after the action executes.

        }
    }
}