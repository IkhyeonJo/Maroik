using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using MyLaboratory.Site.Models;
using System.Text;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Data;
using System.Linq;

namespace MyLaboratory.Site.Filters
{
    public class GlobalActionFilter : ActionFilterAttribute
    {
        // Do something before the action executes.
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string currentControllerName = context.ActionDescriptor.RouteValues["controller"].ToString(); // 실행 전 Controller 이름
            string currentActionName = context.ActionDescriptor.RouteValues["action"].ToString(); // 실행 전 Action 이름
            string currentRequestMethod = context.HttpContext.Request.Method; // GET HTTP METHOD
            bool isAccountSessionExist = context.HttpContext.Session.TryGetValue("AccountSession", out _); // 로그인이 되어 계정 세션이 생겼는지 확인

            if (currentRequestMethod.ToUpper() == "GET") // GET 방식 (페이지 접근만 차단!!)
            {
                if (isAccountSessionExist == false) // 로그인이 안 된 상태이면
                {
                    if (currentControllerName == "Exception" && currentActionName == "Error") // 예외처리 자원 접근 허용 (Error)
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "Login") // 로그인 페이지인 경우
                        {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "Register") // 회원가입 페이지인 경우
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "IDInquiry") // 아이디 찾기 페이지인 경우
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "ForgotPassword") // 비밀번호 찾기 페이지인 경우
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "Logout") // 로그아웃인 경우
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else // 그 외 페이지는 전부 접근 불가!!
                    {
                        context.Result = new UnauthorizedObjectResult("User is unauthorized");
                        return;
                    }
                }
                else if (isAccountSessionExist == true) // 로그인이 된 상태라면
                {
                    if (currentControllerName == "Exception" && currentActionName == "Error") // 예외처리 자원 접근 허용 (Error)
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "Login") // (로그인 된 상태에서 뒤로가기로 로그인 페이지에 갈 수 있음)
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }
                    else if (currentControllerName == "Account" && currentActionName == "Logout") // 로그아웃
                    {
                        base.OnActionExecuting(context); // Do Action!!
                        return;
                    }

                    byte[] resultByte = null;
                    context.HttpContext.Session.TryGetValue("AccountSession", out resultByte);
                    var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                    if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                    {
                        var adminCategories = DbCache.AdminCategories;
                        var adminSubCategories = DbCache.AdminSubCategories;

                        #region SubCategory가 없는 Category일 경우
                        var singleCategories = from adminCategory in adminCategories
                                               where adminCategory.Action != null
                                               select adminCategory; // 서브 카테고리 없는 카테고리 모음.

                        foreach (var singleCategory in singleCategories)
                        {
                            if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                            {
                                base.OnActionExecuting(context); // Do Action!!
                                return;
                            }
                        }
                        #endregion

                        #region SubCategory가 있는 Category일 경우
                        var nonSingleCategories = from adminCategory in adminCategories
                                                  where adminCategory.Action == null
                                                  select adminCategory; // 서브 카테고리 있는 카테고리 모음.

                        foreach (var nonSingleCategory in nonSingleCategories)
                        {
                            var nonSingleCategorysSubCategories = from adminSubCategory in adminSubCategories
                                                                  where adminSubCategory.CategoryId == nonSingleCategory.Id
                                                                  select adminSubCategory;
                            foreach (var nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
                            {
                                if (nonSingleCategory.Controller == currentControllerName && nonSingleCategorysSubCategory.Action == currentActionName)
                                {
                                    base.OnActionExecuting(context); // Do Action!!
                                    return;
                                }
                            }
                        }
                        #endregion
                        context.Result = new UnauthorizedObjectResult("User is unauthorized"); // 다 검사했는데도 나오지 않은 경우는 DB에 없는 경우이다.
                        return;
                        //DB Category & SubCategory 다 뽑아 현재 Access한 Controller & Action과 다 맞는지 비교하고 맞으면 넘어가고 아니면 UnAuthroizedObjectReslut 출력 (Action 취소!!)
                    }
                    else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                    {
                        var userCategories = DbCache.UserCategories;
                        var userSubCategories = DbCache.UserSubCategories;

                        #region SubCategory가 없는 Category일 경우
                        var singleCategories = from userCategory in userCategories
                                               where userCategory.Action != null
                                               select userCategory; // 서브 카테고리 없는 카테고리 모음.

                        foreach (var singleCategory in singleCategories)
                        {
                            if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                            {
                                base.OnActionExecuting(context); // Do Action!!
                                return;
                            }
                        }
                        #endregion

                        #region SubCategory가 있는 Category일 경우
                        var nonSingleCategories = from userCategory in userCategories
                                                  where userCategory.Action == null
                                                  select userCategory; // 서브 카테고리 있는 카테고리 모음.

                        foreach (var nonSingleCategory in nonSingleCategories)
                        {
                            var nonSingleCategorysSubCategories = from userSubCategory in userSubCategories
                                                                  where userSubCategory.CategoryId == nonSingleCategory.Id
                                                                  select userSubCategory;
                            foreach (var nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
                            {
                                if (nonSingleCategory.Controller == currentControllerName && nonSingleCategorysSubCategory.Action == currentActionName)
                                {
                                    base.OnActionExecuting(context); // Do Action!!
                                    return;
                                }
                            }
                        }
                        #endregion
                        context.Result = new UnauthorizedObjectResult("User is unauthorized"); // 다 검사했는데도 나오지 않은 경우는 DB에 없는 경우이다.
                        return;
                        //DB Category & SubCategory 다 뽑아 현재 Access한 Controller & Action과 다 맞는지 비교하고 맞으면 넘어가고 아니면 UnAuthroizedObjectReslut 출력 (Action 취소!!)
                    }
                }
            }
            else // (POST / PATCH / DELETE 등 방식은 다 허용)
            {
                base.OnActionExecuting(context); // Do Action!!
                return;
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