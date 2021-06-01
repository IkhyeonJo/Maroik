using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FESTRubricWebServer.Models;
using FESTRubricWebServer.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FESTRubricWebServer.Controllers
{

    public class UserController : Controller
    {
        private static UserContext Context { get; set; }

        #region 초기 페이지
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region ReadMe
        /// <summary>
        /// ReadMe1
        /// </summary>
        /// <returns></returns>
        public IActionResult ReadMe1()
        {
            return View();
        }
        /// <summary>
        /// ReadMe1 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ReadMe1(int dontCare)
        {
            //SQL문 넣자 동의 컬럼

            return RedirectToAction("ReadMe2", "User");
        }

        /// <summary>
        /// ReadMe2
        /// </summary>
        /// <returns></returns>
        public IActionResult ReadMe2()
        {
            return View();
        }
        /// <summary>
        /// ReadMe1 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ReadMe2(int dontCare)
        {
            return RedirectToAction("Questionnaire", "User");
        }
        #endregion

        #region 설문조사
        /// <summary>
        /// Questionnaire
        /// </summary>
        /// <returns></returns>
        public IActionResult Questionnaire()
        {
            return View();
        }
        /// <summary>
        /// ReadMe1 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Questionnaire(QuestionnaireInputModel questionnaireInputModel)
        {
            if (Context == null)
            {
                Context = HttpContext.RequestServices.GetService(typeof(FESTRubricWebServer.Models.User.UserContext)) as UserContext;
            }

            if (ModelState.IsValid)
            {
                Context.CreateQuestionnaire(questionnaireInputModel);
                return RedirectToAction("Final", "User");
            }
            return View(questionnaireInputModel);
        }
        #endregion

        #region Final
        /// <summary>
        /// Final
        /// </summary>
        /// <returns></returns>
        public IActionResult Final()
        {
            return View();
        }
        #endregion
    }
}