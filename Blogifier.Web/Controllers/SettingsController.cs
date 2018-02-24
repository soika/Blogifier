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
    using Core.Middleware;
    using Core.Services.Email;
    using Core.Services.FileSystem;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Models;
    using Models.AccountViewModels;
    using Models.Admin;

    [Authorize]
    [Route("admin/[controller]")]
    public class SettingsController : Controller
    {
        private readonly IUnitOfWork db;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailService emailSender;
        private readonly ILogger logger;
        private readonly string theme;
        private readonly string pwdTheme = "~/Views/Account/ChangePassword.cshtml";

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailSender,
            ILogger<AccountController> logger,
            IUnitOfWork db
            )
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
            this.logger = logger;
            this.db = db;
            this.theme = $"~/{ApplicationSettings.BlogAdminFolder}/";
        }

        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        [MustBeAdmin]
        [Route("users")]
        public IActionResult Users(int page = 1)
        {
            var profile = GetProfile();
            var pager = new Pager(page);
            var blogs = this.db.Profiles.ProfileList(p => p.Id > 0, pager);

            var model = GetUsersModel();
            model.Blogs = blogs;
            model.Pager = pager;

            return View(this.theme + "Settings/Users.cshtml", model);
        }

        [HttpPost]
        [MustBeAdmin]
        [ValidateAntiForgeryToken]
        [Route("users")]
        public async Task<IActionResult> Register(UsersViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["AdminPage"] = true;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.RegisterModel.Email, Email = model.RegisterModel.Email };
                var result = await this.userManager.CreateAsync(user, model.RegisterModel.Password);
                if (result.Succeeded)
                {
                    this.logger.LogInformation(string.Format("Created a new account for {0}", user.UserName));

                    // create new profile
                    var profile = new Profile();

                    if (this.db.Profiles.All().ToList().Count == 0 || model.RegisterModel.IsAdmin)
                    {
                        profile.IsAdmin = true;
                    }

                    profile.AuthorName = model.RegisterModel.AuthorName;
                    profile.AuthorEmail = model.RegisterModel.Email;
                    profile.Title = "New blog";
                    profile.Description = "New blog description";

                    profile.IdentityName = user.UserName;
                    profile.Slug = SlugFromTitle(profile.AuthorName);
                    profile.Avatar = ApplicationSettings.ProfileAvatar;
                    profile.BlogTheme = BlogSettings.Theme;

                    profile.LastUpdated = SystemClock.Now();

                    this.db.Profiles.Add(profile);
                    this.db.Complete();

                    this.logger.LogInformation(string.Format("Created a new profile at /{0}", profile.Slug));

                    if (model.RegisterModel.SendEmailNotification)
                    {
                        var userUrl = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, profile.Slug);
                        await this.emailSender.SendEmailWelcomeAsync(model.RegisterModel.Email, model.RegisterModel.AuthorName, userUrl);
                    }

                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            var pager = new Pager(1);
            var blogs = this.db.Profiles.ProfileList(p => p.Id > 0, pager);

            var regModel = GetUsersModel();
            regModel.Blogs = blogs;
            regModel.Pager = pager;

            return View(this.theme + "Settings/Users.cshtml", regModel);
        }

        [MustBeAdmin]
        [HttpDelete("{id}")]
        [Route("users/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = GetProfile();

            if (!admin.IsAdmin || admin.Id == id)
                return NotFound();

            var profile = this.db.Profiles.Single(p => p.Id == id);

            this.logger.LogInformation(string.Format("Delete blog {0} by {1}", profile.Title, profile.AuthorName));

            var assets = this.db.Assets.Find(a => a.ProfileId == id);
            this.db.Assets.RemoveRange(assets);
            this.db.Complete();
            this.logger.LogInformation("Assets deleted");

            var categories = this.db.Categories.Find(c => c.ProfileId == id);
            this.db.Categories.RemoveRange(categories);
            this.db.Complete();
            this.logger.LogInformation("Categories deleted");

            var posts = this.db.BlogPosts.Find(p => p.ProfileId == id);
            this.db.BlogPosts.RemoveRange(posts);
            this.db.Complete();
            this.logger.LogInformation("Posts deleted");

            var fields = this.db.CustomFields.Find(f => f.CustomType == CustomType.Profile && f.ParentId == id);
            this.db.CustomFields.RemoveRange(fields);
            this.db.Complete();
            this.logger.LogInformation("Custom fields deleted");

            var profileToDelete = this.db.Profiles.Single(b => b.Id == id);

            var storage = new BlogStorage(profileToDelete.Slug);
            storage.DeleteFolder("");
            this.logger.LogInformation("Storage deleted");

            this.db.Profiles.Remove(profileToDelete);
            this.db.Complete();
            this.logger.LogInformation("Profile deleted");

            // remove login

            var user = await this.userManager.FindByNameAsync(profile.IdentityName);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{this.userManager.GetUserId(User)}'.");
            }
            var result = await this.userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing login for user with ID '{user.Id}'.");
            }
            return new NoContentResult();
        }

        [HttpGet]
        [Route("changepassword")]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await this.userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{this.userManager.GetUserId(User)}'.");
            }

            var hasPassword = await this.userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction("Login", "Account");
            }

            var profile = this.db.Profiles.Single(p => p.IdentityName == User.Identity.Name);
            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage, Profile = profile };
            return View(this.pwdTheme, model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("changepassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            model.Profile = GetProfile();

            if (!ModelState.IsValid)
            {
                return View(this.pwdTheme, model);
            }

            var user = await this.userManager.GetUserAsync(User);
            if (user == null)
            {
                model.StatusMessage = $"Error: Unable to load user with ID '{this.userManager.GetUserId(User)}'";
                return View(this.pwdTheme, model);
            }

            var changePasswordResult = await this.userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                model.StatusMessage = $"Error: {changePasswordResult.Errors.ToList()[0].Description}";
                return View(this.pwdTheme, model);
            }

            await this.signInManager.SignInAsync(user, isPersistent: false);
            this.logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }

        #region Helpers

        private Profile GetProfile()
        {
            return this.db.Profiles.Single(b => b.IdentityName == User.Identity.Name);
        }

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

        UsersViewModel GetUsersModel()
        {
            var profile = GetProfile();

            var model = new UsersViewModel
            {
                Profile = profile,
                RegisterModel = new RegisterViewModel()
            };
            model.RegisterModel.SendGridApiKey = this.db.CustomFields.GetValue(
                CustomType.Application, profile.Id, Constants.SendGridApiKey);

            return model;
        }

        #endregion
    }
}