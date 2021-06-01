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
        private static AdminContext Context { get; set; }
        private const string AdminSessionKey = "FestAdminSession";

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
                    ModelState.AddModelError(string.Empty, "ID 또는 비밀번호가 틀렸습니다. (ID or Password is Wrong.)");
                    return View(adminLoginInputModel);
                }
                Context.GetAdmin(adminLoginInputModel);
                
                HttpContext.Session.SetString(AdminSessionKey, new StringBuilder().Append(adminLoginInputModel.Category).Append("~").Append(adminLoginInputModel.Level).ToString());
                
                return RedirectToAction("Index", "Admin");
            }
            return View(adminLoginInputModel);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AdminSessionKey);
            return RedirectToAction("Login", "Admin");
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
                return RedirectToAction("Login", "Admin");

            return View(Context.GetAllQuestionnaire());
        }
        [HttpPost]
        public IActionResult Index(List<QuestionnaireInputModel> listItem)
        {
            if (HttpContext.Session.GetString(AdminSessionKey) == null)
                return RedirectToAction("Login", "Admin");

            var checkedBoxList = from list in listItem
                                 where list.Checked == true
                                 select list;
            var uncheckedBoxList = from list in listItem
                                   where list.Checked == false
                                   select list;

            foreach (var checkedBox in checkedBoxList)
            {
                Context.IndexSave(checkedBox, "입장");
            }
            foreach (var uncheckedBox in uncheckedBoxList)
            {
                Context.IndexSave(uncheckedBox, "등록");
            }

            return RedirectToAction("Index", "Admin");
        }

    }
}