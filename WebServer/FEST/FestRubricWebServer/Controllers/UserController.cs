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
        private const string SessionKey = "FESTSession";
        #region Login/ReadMe/Logout/Register/Reset Password Legion
        /// <summary>
        /// 로그인
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            if (Context == null)
            {
                Context = HttpContext.RequestServices.GetService(typeof(FESTRubricWebServer.Models.User.UserContext)) as UserContext;
            }
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            return View();
        }

        /// <summary>
        /// 로그인 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Login(LoginInputModel loginInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }

            if (ModelState.IsValid)
            {
                if (!(Context.IsRegistered(loginInputModel) > 0))
                {
                    ModelState.AddModelError(string.Empty, "ID 또는 팀 번호 또는 비밀번호가 틀렸습니다. (ID or Team Number or Password is Wrong.)");
                    return View(loginInputModel);
                }

                #region 개인정보 수집 동의 확인 여부
                if (!Context.IsAgreedContractor(loginInputModel))
                {
                    HttpContext.Session.SetString(SessionKey, loginInputModel.TeamNo.ToString()); // Create Session.
                    return RedirectToAction("Notice", "User");
                }
                else
                {
                    TempLoginInputModel.CoachEmail = loginInputModel.CoachEmail;
                    TempLoginInputModel.TeamNo = loginInputModel.TeamNo;
                    TempLoginInputModel.CoachPassword = loginInputModel.CoachPassword;
                    return RedirectToAction("ReadMe", "User");
                }
                #endregion
            }
            return View(loginInputModel);
        }

        /// <summary>
        /// ReadMe
        /// </summary>
        /// <returns></returns>
        public IActionResult ReadMe()
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            return View();
        }
        /// <summary>
        /// ReadMe Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ReadMe(int dontCare)
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            LoginInputModel loginInputModel = new LoginInputModel
            {
                CoachEmail = TempLoginInputModel.CoachEmail,
                TeamNo = TempLoginInputModel.TeamNo,
                CoachPassword = TempLoginInputModel.CoachPassword
            };

            TempLoginInputModel.CoachEmail = null;
            TempLoginInputModel.TeamNo = -9999;
            TempLoginInputModel.CoachPassword = null;

            if (!(Context.IsRegistered(loginInputModel) > 0))
            {
                return RedirectToAction("Login", "User");
            }
            Context.SetPersonalInformation(loginInputModel);
            
            HttpContext.Session.SetString(SessionKey, loginInputModel.TeamNo.ToString()); // Create Session.
            
            return RedirectToAction("Notice", "User");
        }
        /// <summary>
        /// 로그아웃
        /// </summary>
        /// <returns></returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction("Login", "User");
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        /// <returns></returns>
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            return View();
        }

        /// <summary>
        /// 회원가입 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Register(RegisterInputModel registerInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            if (ModelState.IsValid)
            {
                if (!Context.IsValidEmailAndTeamNo(registerInputModel))
                {
                    ModelState.AddModelError(string.Empty, "이메일 주소 또는 팀 번호가 유효하지 않습니다. \n(Invalid Email Address or Team Number.)");
                    return View(registerInputModel);
                }
                else if (!Context.IsKRCRegistered(registerInputModel))
                {
                    ModelState.AddModelError(string.Empty, "KRC에 등록되지 않은 이메일 및 팀 번호입니다. 관리자에게 문의 바랍니다. \n(Email Address & Number did not register in KRC. please contact with administrator.)");
                    return View(registerInputModel);
                }
                else if (!Context.IsPasswordExist(registerInputModel))
                {
                    ModelState.AddModelError(string.Empty, "이미 등록된 이메일 주소 및 팀 번호입니다. 로그인 해주세요. \n(Already registered Email Address And Team Number. Please Do Login)");
                    return View(registerInputModel);
                }

                #region 비밀번호 설정 시작
                if (Context.SetPassword(registerInputModel) > 0)
                    ModelState.AddModelError(string.Empty, "회원 가입 성공. (Registration Done.)");
                else
                    ModelState.AddModelError(string.Empty, "회원 가입 실패. (Registration Fail.)");
                #endregion
            }
            return View(registerInputModel);    
        }

        /// <summary>
        /// 비밀번호 초기화
        /// </summary>
        /// <returns></returns>
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            return View();
        }

        /// <summary>
        /// 비밀번호 초기화 submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordInputModel resetPasswordInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) != null) // 세션 초기화
            {
                HttpContext.Session.Remove(SessionKey);
            }
            if (ModelState.IsValid)
            {
                if (!Context.IsValidEmailAndTeamNo(resetPasswordInputModel))
                {
                    ModelState.AddModelError(string.Empty, "이메일 주소 또는 팀 번호가 유효하지 않습니다. \n(Invalid Email Address or Team Number.)");
                    return View(resetPasswordInputModel);
                }

                #region 비밀번호 설정 시작
                if (Context.ResetPassword(resetPasswordInputModel) > 0)
                    ModelState.AddModelError(string.Empty, "비밀번호 초기화 성공!! 회원가입에서 비밀번호 재설정 하시길 바랍니다. (ResetPassword Complete!! Please move to register page and set your password)");
                else
                    ModelState.AddModelError(string.Empty, "비밀번호 초기화 실패!! (ResetPassword Failed!!)");
                #endregion
            }
            return View(resetPasswordInputModel);
        }
        #endregion

        #region Notice

        /// <summary>
        /// 공지사항
        /// </summary>
        /// <returns></returns>
        public IActionResult Notice()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            #region 해야할 일 체크
            #region 팀 제대로 등록 했는 지 체크. (FLL Jr.: 1~6, FLL : 2~10, FTC : 3 ~ 15)
            var tempModel = Context.GetCategoryAndYear(HttpContext.Session.GetString(SessionKey)); // 20181211 조익현 작성. PK인 TeamNo로 Category를 구합니다.
            int rowCount = Context.GetRegisteredMemberNumber(HttpContext.Session.GetString(SessionKey)); // 20181211 조익현 작성. 몇 명의 멤버를 등록했는 지 구합니다.

            ViewBag.RegisteredMemberNumber = rowCount;

            if (tempModel.Category.Equals("FLL Jr."))
            {
                if (rowCount >= 1 && rowCount <= 6)
                    ViewBag.IsMemberRegisterDone = "O";
                else
                    ViewBag.IsMemberRegisterDone = "X";
            }
            else if(tempModel.Category.Equals("FLL"))
            {
                if (rowCount >= 2 && rowCount <= 10)
                    ViewBag.IsMemberRegisterDone = "O";
                else
                    ViewBag.IsMemberRegisterDone = "X";
            }
            else if (tempModel.Category.Equals("FTC"))
            {
                if (rowCount >= 3 && rowCount <= 15)
                    ViewBag.IsMemberRegisterDone = "O";
                else
                    ViewBag.IsMemberRegisterDone = "X";
            }

            #endregion

            #region IsKRCTeamRegistration Setting (팀 등록 전부 끝난 후 뒤늦게 컬럼 추가해서 이런 사태가 발생...) 20190115 조익현
            Context.SetIsKRCTeamRegistration(HttpContext.Session.GetString(SessionKey), ViewBag.IsMemberRegisterDone);
            #endregion

            #region Print IsKRCColumns
            ViewBag.IsKRCWrittenOath = Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsKRCWrittenOath; //KRC 팀 서약서 제출 여부
            ViewBag.IsKRCAdmissionTicket = Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsKRCAdmissionTicket; // KRC 팀 입장권 출력 여부
            ViewBag.IsFTCTeamDocuments = Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsFTCTeamDocuments; // FTC 팀 서류 제출 여부
            #endregion
            #endregion
            return View();
        }
        #endregion

        #region 선수 등록 CRUD
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            ViewBag.RegisteredMemberNumber = Context.GetRegisteredMemberNumber(HttpContext.Session.GetString(SessionKey));
            ViewBag.Category = Context.GetCategoryAndYear(HttpContext.Session.GetString(SessionKey)).Category;
            return View(Context.GetAllMember(HttpContext.Session.GetString(SessionKey)));
        }

        public IActionResult Edit(int? Id)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("Index", "User");

            var retModel = Context.GetMember(Id);

            if (string.IsNullOrEmpty(retModel.ResidentNumber))
                retModel.IsForeigner = true;
            else
                retModel.IsForeigner = false;

            return View(retModel);
        }
        [HttpPost]
        public IActionResult Edit(MemberInputModel memberInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("Index", "User");
            if (ModelState.IsValid)
            {
                Context.UpdateMember(memberInputModel);
                return RedirectToAction("Index", "User");
            }
            return View(memberInputModel);
        }

        public IActionResult Delete(int? Id)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("Index", "User");
            Context.DeleteMember(Id);
            return RedirectToAction("Index", "User");
        }

        public IActionResult Create()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("Index", "User");
            return View();
        }

        [HttpPost]
        public IActionResult Create(MemberInputModel memberInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("Index", "User");
            if (ModelState.IsValid)
            {
                #region 팀 제대로 등록 했는 지 체크. (FLL Jr.: 1~6, FLL : 2~10, FTC : 3 ~ 15)
                var tempModel = Context.GetCategoryAndYear(HttpContext.Session.GetString(SessionKey)); // 20181211 조익현 작성. PK인 TeamNo로 Category를 구합니다. 
                int rowCount = Context.GetRegisteredMemberNumber(HttpContext.Session.GetString(SessionKey)); // 20181211 조익현 작성. 몇 명의 멤버를 등록 했는 지 구합니다.

                if (tempModel.Category.Equals("FLL Jr.")) // 20181211 
                {
                    if (rowCount < 6)
                        Context.CreateMember(memberInputModel, tempModel.Category, tempModel.Year, HttpContext.Session.GetString(SessionKey));
                    else
                    {
                        ModelState.AddModelError(string.Empty, "FLL Jr. 등록 범위 : 1 ~ 6 (FLL Jr. Range of registration : 1 ~ 6)");
                        return View(memberInputModel);
                    }
                }
                else if (tempModel.Category.Equals("FLL")) // 20181211 Test Done.
                {
                    if (rowCount < 10)
                        Context.CreateMember(memberInputModel, tempModel.Category, tempModel.Year, HttpContext.Session.GetString(SessionKey));
                    else
                    {
                        ModelState.AddModelError(string.Empty, "FLL 등록 범위 : 2 ~ 10 (FLL Range of registration : 2 ~ 10)");
                        return View(memberInputModel);
                    }
                }
                else if (tempModel.Category.Equals("FTC")) // 20181211 Test Done.
                {
                    if (rowCount < 15)
                        Context.CreateMember(memberInputModel, tempModel.Category, tempModel.Year, HttpContext.Session.GetString(SessionKey));
                    else
                    {
                        ModelState.AddModelError(string.Empty, "FTC 등록 범위 : 3 ~ 15 (FTC Range of registration : 3 ~ 15)");
                        return View(memberInputModel);
                    }
                }
                #endregion
                
                return RedirectToAction("Index", "User");
            }
            return View(memberInputModel);
        }
        #endregion

        #region 코치
        #region 코치 정보
        public IActionResult CoachInfo()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            return View(Context.GetCoachInfo(HttpContext.Session.GetString(SessionKey)));
        }
        #endregion
        #region 코치 편집
        public IActionResult CoachEdit()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("CoachInfo", "User");
            return View(Context.GetEditCoachInfo(HttpContext.Session.GetString(SessionKey)));
        }
        #endregion
        #region 코치 편집 Save
        [HttpPost]
        public IActionResult CoachEdit(CoachInputModel coachInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("CoachInfo", "User");
            Context.EditCoachInfo(coachInputModel, HttpContext.Session.GetString(SessionKey));
            return RedirectToAction("Notice", "User");
        }
        #endregion
        #endregion

        #region 팀 정보 (team Information)
        public IActionResult TeamInfo()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            return View(Context.GetTeamInfo(HttpContext.Session.GetString(SessionKey)));
        }
        /// <summary>
        /// 팀 정보 편집
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult EditTeamInfo()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("TeamInfo", "User");
            return View(Context.GetEditTeamInfo(HttpContext.Session.GetString(SessionKey)));
        }

        /// <summary>
        /// 팀 정보 편집 Submit
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult EditTeamInfo(TeamInputModel teamInputModel)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2018, 12, 29)) >= 0)
                return RedirectToAction("TeamInfo", "User");
            Context.EditTeamInfo(teamInputModel);
            return RedirectToAction("Notice", "User");
        }
        #endregion

        #region KRC WrittenOath
        /// <summary>
        /// KRC 서약서 제출
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult KRCWrittenOath(/*string result=""*/)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if ((DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 26)) >= 0) && (DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 13)) < 0)) // 2019.1.13 ~ 2019.1.26
                return RedirectToAction("Notice", "User");
            //ViewBag.Result = result;
            //if(Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsKRCTeamRegistration == "X")
            //{
            //    TempData["check"] = @"<script>alert('먼저 팀 등록을 조건에 맞게 진행하여주십시오.');</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
            //    return View();
            //} //도교수님 부탁으로 팀 등록 안 한 팀도 서약서 제출 되게 합니다. 20190119 조익현 작성.
            
            return View();
        }
        /// <summary>
        /// KRC 서약서 업로드
        /// </summary>
        /// <returns></returns>
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> KRCWrittenOath(List<IFormFile> files, string chk_info)
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if ((DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 26)) >= 0) && (DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 13)) < 0)) // 2019.1.13 ~ 2019.1.26
                return RedirectToAction("Notice", "User");
            //if (Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsKRCTeamRegistration == "X")
            //    return RedirectToAction("Notice", "User"); //도교수님 부탁으로 팀 등록 안 한 팀도 서약서 제출 되게 합니다. 20190119 조익현 작성.
            if (chk_info == "Agree")
            {
                foreach (var file in files)
                {
                    if (file.Length > 0 && file.Length <= 10485760) // file Length : 파일 크기 10MB 이하
                    {
                        // Notice : 해당 경로를 chmod 777로 해서 권한을 최고로 높일 것
                        using (var stream = new FileStream(@"/home/KRCUploads/" + HttpContext.Session.GetString(SessionKey) + "-" + Path.GetFileName(file.FileName), FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            await file.CopyToAsync(stream);
                        }
                        Context.SetIsKRCWrittenOath(HttpContext.Session.GetString(SessionKey));
                        TempData["msg"] = @"<script>alert('서약서 제출이 완료 되었습니다.');</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
                    }
                    else if (!(file.Length <= 10485760))
                    {
                        return RedirectToAction("KRCWrittenOath", "User"/*, new { result = "파일 크기는 10MB 이하만 가능합니다. (File Size must be smaller than 10MB.)" }*/);
                    }
                    else
                    {
                        return RedirectToAction("KRCWrittenOath", "User"/*, new { result = "Fail to File upload (UNKNOWN CASE)" }*/);
                    }
                }
            }
            else if(chk_info == "Disagree")
            {
                return RedirectToAction("KRCWrittenOath", "User"/*, new { result = "서약에 동의하여 주십시오. (Please agree to the pledge.)" }*/);
            }
            return RedirectToAction("KRCWrittenOath", "User"/*, new { result = "파일 첨부하여 주십시오. (Please attach a file)" }*/);
        }
        #endregion

        #region Admission Ticket
        /// <summary>
        /// 팀 입장권
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult KRCAdmissionTicket()
        {
            if (HttpContext.Session.GetString(SessionKey) == null)
                return RedirectToAction("Login", "User");
            if ((DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 26)) >= 0) && (DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 13)) < 0)) // 2019.1.13 ~ 2019.1.26
                return RedirectToAction("Notice", "User");
            if (Context.GetIsNoticeColumns(HttpContext.Session.GetString(SessionKey)).IsKRCWrittenOath == "X")
            {
                TempData["check"] = @"<script>alert('먼저 KRC 팀 서약서를 진행하여주십시오.');</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
                return View(new AdmissionTicketOutputModel());
            }
            AdmissionTicketOutputModel admissionTicketOutputModel = new AdmissionTicketOutputModel(); // 20181224 조익현. View로 전달할 Instance.

            #region 팀 번호로 그 팀에 해당하는 카테고리 구함
            admissionTicketOutputModel.Category = Context.GetCategoryAndYear(HttpContext.Session.GetString(SessionKey)).Category;
            #endregion

            #region Category에 해당하는 Teamparticipation DB 열 내용 전부 팀 번호 오른차순으로 구한 후 팀 번호에 해당하는 rowNumber 부여(3 digit) 및 CoachName, Coach Email 기억. 
            int rowNumber = 1;
            foreach (var CategoryCoachInfo in Context.GetAllCategoryCoachInfo(admissionTicketOutputModel.Category))
            {
                if (CategoryCoachInfo.TeamNo == Convert.ToInt32(HttpContext.Session.GetString(SessionKey)))
                {
                    admissionTicketOutputModel.RowNumber = rowNumber.ToString("D3");
                    admissionTicketOutputModel.CoachName = CategoryCoachInfo.CoachName;
                    admissionTicketOutputModel.CoachEmail = CategoryCoachInfo.CoachEmail;
                    break;
                }
                rowNumber++;
            }
            #endregion

            #region EntranceGate 결정
            if (admissionTicketOutputModel.Category == "FLL Jr.")
                admissionTicketOutputModel.EntranceGate = "A";
            else if (((admissionTicketOutputModel.Category == "FLL") && (rowNumber >= 1 && rowNumber <= 70)))
                admissionTicketOutputModel.EntranceGate = "B";
            else // FLL rowNumber 71~100 or FTC
                admissionTicketOutputModel.EntranceGate = "C";
            #endregion

            #region 몇 명의 멤버를 등록했는 지 구합니다.
            admissionTicketOutputModel.TotalMemberNum = Context.GetRegisteredMemberNumber(HttpContext.Session.GetString(SessionKey));
            #endregion

            #region 팀번호 대입
            admissionTicketOutputModel.TeamNo = Convert.ToInt32(HttpContext.Session.GetString(SessionKey));
            #endregion

            #region POP UP Print & Setting IsKRCWrittenOath Flag
            TempData["print"] = @"<script>window.print();</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
            Context.SetIsKRCAdmissionTicket(HttpContext.Session.GetString(SessionKey));
            #endregion
            return View(admissionTicketOutputModel);
        }

        #endregion

        #region FTC 팀 서류 제출
        /// <summary>
        /// FTC 팀 서류 제출
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult FTCTeamDocuments()
        {
            if (Convert.ToInt32(HttpContext.Session.GetString(SessionKey)) / 1000 != 5) // FTC만 이 Action에 도달할 수 있습니다.
                return RedirectToAction("Notice", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 25)) >= 0) //기한 : 2019.1.24
                TempData["check"] = @"<script>alert('기한 초과되었습니다. (기한 : 2019.1.24)');</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
            
            return View();
        }
        /// <summary>
        /// FTC 팀 서류 업로드
        /// </summary>
        /// <returns></returns>
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> FTCTeamDocuments(List<IFormFile> files)
        {
            if (Convert.ToInt32(HttpContext.Session.GetString(SessionKey)) / 1000 != 5) // FTC만 이 Action에 도달할 수 있습니다.
                return RedirectToAction("Notice", "User");
            if (DateTime.Compare(DateTime.Now, new DateTime(2019, 1, 25)) >= 0) //기한 : 2019.1.24
                return RedirectToAction("Notice", "User");
            
            foreach (var file in files)
            {
                if (file.Length > 0 && file.Length <= 10485760) // file Length : 파일 크기 10MB 이하
                {
                    // Notice : 해당 경로를 chmod 777로 해서 권한을 최고로 높일 것
                    using (var stream = new FileStream(@"/home/FTCUploads/" + HttpContext.Session.GetString(SessionKey) + "-" + Path.GetFileName(file.FileName), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await file.CopyToAsync(stream);
                    }
                    Context.SetIsFTCTeamDocuments(HttpContext.Session.GetString(SessionKey));
                    TempData["msg"] = @"<script>alert('FTC 팀 서류 제출이 완료 되었습니다.');</script>"; //https://stackoverflow.com/questions/22657335/how-to-show-alert-message-in-mvc-4-controller/22657456
                }
                else if (!(file.Length <= 10485760))
                {
                    return RedirectToAction("FTCTeamDocuments", "User"/*, new { result = "파일 크기는 10MB 이하만 가능합니다. (File Size must be smaller than 10MB.)" }*/);
                }
                else
                {
                    return RedirectToAction("FTCTeamDocuments", "User"/*, new { result = "Fail to File upload (UNKNOWN CASE)" }*/);
                }
            }    
            return RedirectToAction("FTCTeamDocuments", "User"/*, new { result = "파일 첨부하여 주십시오. (Please attach a file)" }*/);
        }
        #endregion
    }
}