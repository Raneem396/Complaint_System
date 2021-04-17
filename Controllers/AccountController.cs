using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using ComplaintManagementPortal2.Models;
using PagedList;
using System.Net;
using System.Data.SqlClient;
using System.Data.Entity.Infrastructure;

namespace ComplaintManagementPortal2.Controllers
{
    [Authorize(Roles = "Admin,Customer")]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {

        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [Route("")]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    var user = UserManager.FindByEmail(model.Email);
                    Response.Cookies["fullname"].Value = user.FullName;
                    user = null;
                    if (returnUrl != null && returnUrl != "")
                        return RedirectToLocal(returnUrl);
                    else
                        return RedirectToAction("Index", "Dashboard");
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("EditMyAccount", new { result = "editpassworddone" });
            }
            AddErrors(result);
            return View(model);
        }


        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            Response.Cookies["fullname"].Value = "";
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }


        //
        // GET: /Account/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            CreateUserViewModel model = new CreateUserViewModel();
            ApplicationDbContext db = new ApplicationDbContext();
            model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name");
            return View(model);
        }

        //
        // POST: /Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var roleResult = UserManager.AddToRole(user.Id, model.RoleName);
                    if (roleResult.Succeeded)
                        return RedirectToAction("Index", new { result = "adddone" });
                    else
                    {
                        AddErrors(roleResult);
                    }
                }
                else
                    AddErrors(result);
            }

            ApplicationDbContext db = new ApplicationDbContext();
            model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name");
            return View(model);
        }

        //Register as a customer
        //
        // GET: /Account/CreateCustomer
        [AllowAnonymous]
        public ActionResult CreateCustomer()
        {
            CreateUserViewModel model = new CreateUserViewModel();
            ApplicationDbContext db = new ApplicationDbContext();
           // model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name");
            return View(model);
        }

        //
        // POST: /Account/CreateCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<ActionResult> CreateCustomer(CreateUserViewModel model)
        {
            model.RoleName = "Customer";
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var roleResult = UserManager.AddToRole(user.Id, "Customer");
                    if (roleResult.Succeeded)
                        return RedirectToAction("Login","Account", new { result = "adddone" });
                    else
                    {
                        AddErrors(roleResult);
                    }
                }
                else
                    AddErrors(result);
            }

            ApplicationDbContext db = new ApplicationDbContext();
           // model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name");
            return View(model);
        }



        // GET: Accounts
        [Authorize(Roles = "Admin")]
        public ActionResult Index(string sortOrder, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "fullname_desc" : "";
            ViewBag.EmailSortParm = sortOrder == "email" ? "email_desc" : "email";

            ApplicationDbContext db = new ApplicationDbContext();


            //var usersWithRoles = (from user in db.Users
            //                      from userRole in user.Roles
            //                      join role in db.Roles on userRole.RoleId equals
            //                      role.Id
            //                      select new UserViewModel()
            //                      {
            //                          Id = user.Id,
            //                          FullName = user.FullName,
            //                          Email = user.Email,
            //                          RoleName = role.Name
            //                      });


            var users = (from user in db.Users
                         from userRole in user.Roles
                         join role in db.Roles on userRole.RoleId equals role.Id
                         where role.Name == "Admin"
                         select new UserInfoViewModel()
                         {
                             Id = user.Id,
                             FullName = user.FullName,
                             Email = user.Email
                         });




            switch (sortOrder)
            {
                case "email_desc":
                    users = users.OrderByDescending(s => s.Email);
                    break;
                case "email":
                    users = users.OrderBy(s => s.Email);
                    break;
                case "fullname_desc":
                    users = users.OrderByDescending(s => s.FullName);
                    break;
                default:  // Name ascending 
                    users = users.OrderBy(s => s.FullName);
                    break;
            }

            int pageSize = GlobalConfig.PaginationPageSize;
            int pageNumber = (page ?? 1);
            return View(users.ToPagedList(pageNumber, pageSize));
        }

        // GET: Account/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = UserManager.FindById(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            EditUserViewModel model = new EditUserViewModel();

            model.RoleName = UserManager.GetRoles(user.Id).Single();

            ApplicationDbContext db = new ApplicationDbContext();
            model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name", model.RoleName);
            model.Id = user.Id;
            model.Email = user.Email;
            model.FullName = user.FullName;

            return View(model);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                //string roleName = Roles.User.ToString();
                //if (model.IsAdmin)
                //    roleName = Roles.Admin.ToString();


                var user = UserManager.FindById(model.Id);
                user.Email = model.Email;
                user.UserName = model.Email;
                user.FullName = model.FullName;

                var result = UserManager.Update(user);

                if (result.Succeeded)
                {
                    string[] allUserRoles = UserManager.GetRoles(user.Id).ToArray();
                    UserManager.RemoveFromRoles(user.Id, allUserRoles);

                    var roleResult = UserManager.AddToRole(user.Id, model.RoleName);
                    if (roleResult.Succeeded)
                        return RedirectToAction("Index", new { result = "editdone" });
                    else
                    {
                        AddErrors(roleResult);
                    }
                }
                AddErrors(result);
            }


            ApplicationDbContext db = new ApplicationDbContext();
            model.AllRoles = new SelectList(db.Roles.OrderBy(x => x.Name), "Name", "Name", model.RoleName);
            return View(model);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(string id)
        {

            var user = UserManager.FindById(id);
            try
            {
                var result = UserManager.Delete(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", new { result = "deletedone" });
                }

            }
            catch (DbUpdateException ex)
            {
                return RedirectToAction("Edit", new { id = id, result = "relatederror" });
            }


            return View();
        }


        // GET: Account/EditMyAccount
        public ActionResult EditMyAccount()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            EditMyAccountViewModel editMyAccountViewModel = new EditMyAccountViewModel();
            editMyAccountViewModel.Email = user.Email;
            editMyAccountViewModel.FullName = user.FullName;
            user = null;
            return View(editMyAccountViewModel);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMyAccount(EditMyAccountViewModel editMyAccountViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                user.Email = editMyAccountViewModel.Email;
                user.UserName = editMyAccountViewModel.Email;
                user.FullName = editMyAccountViewModel.FullName;

                var result = UserManager.Update(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("EditMyAccount", new { result = "editdone" });
                }
                AddErrors(result);
            }

            return View(editMyAccountViewModel);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }



        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}