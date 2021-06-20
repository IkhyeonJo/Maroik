using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using MyLaboratory.Site.Models;
using System.Text;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Data;

namespace MyLaboratory.Site.Filters
{
    public class GlobalActionFilter : ActionFilterAttribute
    {
        // Do something before the action executes.
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string controllerName = context.ActionDescriptor.RouteValues["controller"].ToString();
            string actionName = context.ActionDescriptor.RouteValues["action"].ToString();

            bool isAccountSessionExist = context.HttpContext.Session.TryGetValue("AccountSession", out _);

            if (isAccountSessionExist == false) // 로그인이 안 된 상태이면
            {
                if (controllerName == "Account" && actionName == "Login") // 로그인 페이지인 경우
                {
                    base.OnActionExecuting(context); // Do Action!!
                }
                else if (controllerName == "Account" && actionName == "Register") // 회원가입 페이지인 경우
                {
                    base.OnActionExecuting(context); // Do Action!!
                }
                else if (controllerName == "Account" && actionName == "IDInquiry") // 아이디 찾기 페이지인 경우
                {
                    base.OnActionExecuting(context); // Do Action!!
                }
                else if (controllerName == "Account" && actionName == "ForgotPassword") // 비밀번호 찾기 페이지인 경우
                {
                    base.OnActionExecuting(context); // Do Action!!
                }
                else // 그 외 페이지는 전부 접근 불가!!
                {
                    context.Result = new UnauthorizedObjectResult("User is unauthorized");
                }
            }
            else if (isAccountSessionExist == true) // 로그인이 된 상태라면
            {
                byte[] resultByte = null;
                context.HttpContext.Session.TryGetValue("AccountSession", out resultByte);
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    var aaa = DbCache.AdminCategories;
                    var bbb = DbCache.AdminSubCategories;
                    var ccc = ";";
                    //DB Category & SubCategory 다 뽑아 현재 Access한 Controller & Action과 다 맞는지 비교하고 맞으면 넘어가고 아니면 UnAuthroizedObjectReslut 출력 (Action 취소!!)
                }
                else if(loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    //DB Category & SubCategory 다 뽑아 현재 Access한 Controller & Action과 다 맞는지 비교하고 맞으면 넘어가고 아니면 UnAuthroizedObjectReslut 출력 (Action 취소!!)
                }
            }

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

            #region Tell GET/POST
            //var a = context.HttpContext.Request.Method; // GET/POST 방식 구하기

            //if (context.HttpContext.Request.Method == "POST")
            //{
            //    // Do nothing
            //}
            //else
            //{
            //    //Do Something
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