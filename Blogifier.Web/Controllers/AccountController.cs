namespace Blogifier.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Common;
    using Core.Controllers;
    using Core.Data.Domain;
    using Core.Data.Interfaces;
    using Core.Extensions;
    using Core.Services.Email;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Models;
    using Models.AccountViewModels;

    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailService emailSender;
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly IUnitOfWork db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailSender,
            IConfiguration config,
            ILogger<AccountController> logger,
            IUnitOfWork db)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
            this.config = config;
            this.logger = logger;
            this.db = db;
        }

        [TempData]
        public string ErrorMessage { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if(this.db.Profiles.All().Count() == 0)
                return RedirectToAction(nameof(AccountController.Register), "Account");

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["ShowRegistration"] = IsFirstAdminAccount();
            ViewData["ShowForgotPwd"] = this.emailSender.Enabled;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await this.signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    this.logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    this.logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            // block registration if admin already added
            if (!IsFirstAdminAccount())
                return View("Error");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await this.userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    this.logger.LogInformation(string.Format("Created a new account for {0}", user.UserName));
                    
                    // create new profile
                    var profile = new Profile();

                    if (this.db.Profiles.All().ToList().Count == 0 || model.IsAdmin)
                    {
                        profile.IsAdmin = true;
                    }
                    
                    profile.AuthorName = model.AuthorName;
                    profile.AuthorEmail = model.Email;
                    profile.Title = "New blog";
                    profile.Description = "New blog description";

                    profile.IdentityName = user.UserName;
                    profile.Slug = SlugFromTitle(profile.AuthorName);
                    profile.Avatar = ApplicationSettings.ProfileAvatar;
                    profile.BlogTheme = BlogSettings.Theme;

                    profile.LastUpdated = Core.Common.SystemClock.Now();

                    this.db.Profiles.Add(profile);
                    this.db.Complete();

                    this.logger.LogInformation(string.Format("Created a new profile at /{0}", profile.Slug));

                    if (model.SendEmailNotification)
                    {
                        var userUrl = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, profile.Slug);
                        await this.emailSender.SendEmailWelcomeAsync(model.Email, model.AuthorName, userUrl);
                    }
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await this.signInManager.SignOutAsync();
            this.logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(BlogController.Index), "Blog");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(BlogController.Index), "Blog");
            }
            var user = await this.userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await this.userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await this.userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var code = await this.userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = ResetPasswordCallbackLink(Url, user.Id, code, Request.Scheme);

                await this.emailSender.Send(model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await this.userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await this.userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(BlogController.Index), "Blog");
            }
        }

        string SlugFromTitle(string title)
        {
            var slug = title.ToSlug();
            if (this.db.Profiles.Single(b => b.Slug == slug) != null)
            {
                for (int i = 2; i < 100; i++)
                {
                    if (this.db.Profiles.Single(b => b.Slug == slug + i.ToString()) == null)
                    {
                        return slug + i.ToString();
                    }
                }
            }
            return slug;
        }

        bool IsFirstAdminAccount()
        {
            // only show registration link if no admin yet exists
            // otherwise new users registered by admin from users panel
            return this.db.Profiles.Find(p => p.IsAdmin).ToList().Count == 0;
        }

        // SendGrid account must be added to configuration settings
        bool EmailServiceEnabled()
        {
            var section = this.config.GetSection("Blogifier");
            if (section == null)
                return false;

            var from = section.GetValue<string>("SendGridEmail");
            var apiKey = section.GetValue<string>("SendGridApiKey");

            return !string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(apiKey);
        }

        static string ResetPasswordCallbackLink(IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ResetPassword),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }

        #endregion
    }
}
