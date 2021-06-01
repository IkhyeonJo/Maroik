using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FESTRubricWebServer.Models.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FESTRubricWebServer.Controllers
{
    public class AdminController : Controller
    {
        #region 2018년도 FEST
        //private static AdminContext Context { get; set; }
        //private const string AdminSessionKey = "FESTAdminSession";

        //public IActionResult Login()
        //{
        //    if (Context == null)
        //    {
        //        Context = HttpContext.RequestServices.GetService(typeof(FESTRubricWebServer.Models.Admin.AdminContext)) as AdminContext;
        //    }
        //    if (HttpContext.Session.GetString(AdminSessionKey) != null) // 세션 초기화
        //    {
        //        HttpContext.Session.Remove(AdminSessionKey);
        //    }
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult Login(AdminLoginInputModel adminLoginInputModel)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (!(Context.IsRegistered(adminLoginInputModel) > 0))
        //        {
        //            ModelState.AddModelError(string.Empty, "ID 또는 비밀번호가 틀렸습니다. (ID or Password is Wrong.)");
        //            return View(adminLoginInputModel);
        //        }
        //        Context.GetAdmin(adminLoginInputModel);
                
        //        HttpContext.Session.SetString(AdminSessionKey, new StringBuilder().Append(adminLoginInputModel.Category).Append("~").Append(adminLoginInputModel.Level).ToString());
                
        //        return RedirectToAction("MenuSelect", "Admin");
        //    }
        //    return View(adminLoginInputModel);
        //}

        //public IActionResult MenuSelect()
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");
        //    return View();
        //}

        //public IActionResult Logout()
        //{
        //    HttpContext.Session.Remove(AdminSessionKey);
        //    return RedirectToAction("Login", "Admin");
        //}

        //public IActionResult Index(string Category)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");
        //    ViewBag.Category = Category;
            
        //    return View(Context.GetAllTeamParticipation(Category));
        //}

        //[HttpPost]
        //public IActionResult KRCSave(List<TeamParticipationOutputModel> listItem)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");
            
        //    var checkedBoxList = from list in listItem
        //                     where list.Checkbox == true
        //                     select list;
        //    var uncheckedBoxList = from list in listItem
        //                           where list.Checkbox == false
        //                           select list;

        //    foreach (var checkedBox in checkedBoxList)
        //    {
        //        Context.AdminKRCSave(checkedBox, "O");
        //    }
        //    foreach (var uncheckedBox in uncheckedBoxList)
        //    {
        //        Context.AdminKRCSave(uncheckedBox, "X");
        //    }

        //    return RedirectToAction("MenuSelect", "Admin");
        //}

        //public IActionResult RegisteredMember(string TeamNo, string Category)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");
        //    ViewBag.TeamNo = TeamNo;
        //    ViewBag.Category = Category;
        //    return View(Context.GetAllMember(TeamNo));
        //}

        //public IActionResult Insurance(string Category)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");
            
        //    ViewBag.Category = Category;
        //    return View(Context.GetInsurance(Category));
        //}

        //public IActionResult ProgramBook(string Category)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");

        //    ViewBag.Category = Category;
        //    return View(Context.GetProgramBook(Category));
        //}

        //public IActionResult CurrentAllRegisteredMember(string Category)
        //{
        //    if (HttpContext.Session.GetString(AdminSessionKey) == null)
        //        return RedirectToAction("Login", "Admin");

        //    ViewBag.Category = Category;
        //    return View(Context.GetCurrentAllRegisteredMember(Category));
        //}
        #endregion

        #region 2019년도 FEST
        private static AdminContext Context { get; set; }
        private const string AdminSessionKey = "FESTAdminSession";
        #region 로그인 관련
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AdminSessionKey);
            return RedirectToAction("Login", "Admin");
        }
        public IActionResult Login()
        {
            if (Context == null)
            {
                Context = HttpContext.RequestServices.GetService(typeof(FESTRubricWebServer.Models.Admin.AdminContext)) as AdminContext;
            }
            if (HttpContext.Session.GetString(AdminSessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(AdminSessionKey);
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(AdminLoginInputModel adminLoginInputModel)
        {
            if (ModelState.IsValid)
            {
                if (!(Context.IsRegistered(adminLoginInputModel) > 0))
                {
                    ModelState.AddModelError(string.Empty, "ID 또는 비밀번호가 틀렸습니다.");
                    return View(adminLoginInputModel);
                }
                Context.GetAdmin(adminLoginInputModel);

                HttpContext.Session.SetString(AdminSessionKey, $"{adminLoginInputModel.Category}~{adminLoginInputModel.Type}");

                return RedirectToAction(adminLoginInputModel.Category, "Admin"); // CoreValue, Design, Project
            }
            return View(adminLoginInputModel);
        }
        #endregion

        #region 핵심가치 심사
        public IActionResult CoreValue()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }
            return View(Context.GetAllRubric("corevaluerubric", HttpContext.Session.GetString(AdminSessionKey).Split("~")[1]));
        }

        public IActionResult CoreValueCreate()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }
            return View();
        }

        [HttpPost]
        public IActionResult CoreValueCreate([FromBody]AdminRubric adminRubric)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }

            if (Context.CreateRubric(adminRubric, "corevaluerubric", HttpContext.Session.GetString(AdminSessionKey).Split("~")[1]) > 0)
            {
                return Json(new { result = true });
            }
            else
            {
                return Json(new { result = false });
            }
        }

        public IActionResult CoreValueEdit(int? id)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }

            var model = Context.GetRubric("corevaluerubric", id).FirstOrDefault();
            ViewBag.BeforeTeamNumber = id;
            return View(model);
        }
        [HttpPost]
        public IActionResult CoreValueEdit([FromBody]AdminRubric adminRubric)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }

            if (Context.UpdateRubric(adminRubric, "corevaluerubric", HttpContext.Session.GetString(AdminSessionKey).Split("~")[1]) > 0)
            {
                return Json(new { result = true });
            }
            else
            {
                return Json(new { result = false });
            }
        }

        public IActionResult CoreValueDelete(int? Id)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }

            Context.DeleteRubric(Id, "corevaluerubric");
            return RedirectToAction("CoreValue", "Admin");
        }

        [HttpPost]
        public IActionResult IsAlreadyCoreValueRubricRegistered(int? teamNumber)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
            {
                return RedirectToAction("Login", "Admin");
            }
            if (!HttpContext.Session.GetString(AdminSessionKey).Contains("CoreValue"))
            {
                return RedirectToAction("Logout", "Admin");
            }

            return Json(new { result = Context.IsAlreadyRubricRegistered(teamNumber, "corevaluerubric") });
        }
        #endregion

        #endregion
    }
}