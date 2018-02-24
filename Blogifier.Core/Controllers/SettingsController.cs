namespace Blogifier.Core.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Data.Domain;
    using Data.Interfaces;
    using Data.Models;
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Middleware;
    using Services.FileSystem;

    [Authorize]
    [Route("admin/[controller]")]
    public class SettingsController : Controller
    {
        private readonly IUnitOfWork db;
        private ILogger logger;
        private readonly string theme;

        public SettingsController(IUnitOfWork db, ILogger<SettingsController> logger)
        {
            this.db = db;
            this.logger = logger;
            this.theme = $"~/{ApplicationSettings.BlogAdminFolder}/Settings/";
        }

        [Route("profile")]
        public IActionResult Profile()
        {
            var model = new SettingsProfile {Profile = GetProfile()};

            if (model.Profile != null)
            {
                model.AuthorName = model.Profile.AuthorName;
                model.AuthorEmail = model.Profile.AuthorEmail;
                model.Avatar = model.Profile.Avatar;
                model.EmailEnabled = this.db.CustomFields.GetValue(CustomType.Application,
                                         0,
                                         Constants.SendGridApiKey).Length > 0;
                model.CustomFields = this.db.CustomFields.GetUserFields(model.Profile.Id).Result;
            }

            return View(this.theme + "Profile.cshtml",
                model);
        }

        [HttpPost]
        [Route("profile")]
        public IActionResult Profile(SettingsProfile model)
        {
            var profile = GetProfile();
            if (ModelState.IsValid)
            {
                if (profile == null)
                {
                    profile = new Profile();

                    if (this.db.Profiles.All().ToList().Count == 0)
                    {
                        profile.IsAdmin = true;
                    }

                    profile.AuthorName = model.AuthorName;
                    profile.AuthorEmail = model.AuthorEmail;
                    profile.Avatar = model.Avatar;

                    profile.IdentityName = User.Identity.Name;
                    profile.Slug = SlugFromTitle(profile.AuthorName);
                    profile.Title = BlogSettings.Title;
                    profile.Description = BlogSettings.Description;
                    profile.BlogTheme = BlogSettings.Theme;

                    this.db.Profiles.Add(profile);
                }
                else
                {
                    profile.AuthorName = model.AuthorName;
                    profile.AuthorEmail = model.AuthorEmail;
                    profile.Avatar = model.Avatar;
                }

                this.db.Complete();

                model.Profile = GetProfile();

                // save custom fields
                if (profile.Id > 0 && model.CustomFields != null)
                {
                    SaveCustomFields(model.CustomFields,
                        profile.Id);
                }

                model.CustomFields = this.db.CustomFields.GetUserFields(model.Profile.Id).Result;

                ViewBag.Message = "Profile updated";
            }

            return View(this.theme + "Profile.cshtml",
                model);
        }

        [VerifyProfile]
        [Route("about")]
        public IActionResult About()
        {
            return View(this.theme + "About.cshtml",
                new AdminBaseModel {Profile = GetProfile()});
        }

        [MustBeAdmin]
        [Route("general")]
        public IActionResult General()
        {
            var profile = GetProfile();

            var model = new SettingsGeneral
            {
                Profile = profile,
                BlogThemes = BlogSettings.BlogThemes,
                Title = BlogSettings.Title,
                Description = BlogSettings.Description,
                BlogTheme = BlogSettings.Theme,
                Logo = BlogSettings.Logo,
                Avatar = ApplicationSettings.ProfileAvatar,
                Image = BlogSettings.Cover,
                EmailKey = this.db.CustomFields.GetValue(CustomType.Application,
                    0,
                    Constants.SendGridApiKey),
                BlogHead = this.db.CustomFields.GetValue(CustomType.Application,
                    0,
                    Constants.HeadCode),
                BlogFooter = this.db.CustomFields.GetValue(CustomType.Application,
                    0,
                    Constants.FooterCode)
            };
            return View(this.theme + "General.cshtml",
                model);
        }

        [HttpPost]
        [MustBeAdmin]
        [Route("general")]
        public IActionResult General(SettingsGeneral model)
        {
            model.BlogThemes = BlogSettings.BlogThemes;
            model.Profile = GetProfile();

            if (ModelState.IsValid)
            {
                BlogSettings.Title = model.Title;
                BlogSettings.Description = model.Description;
                BlogSettings.Logo = model.Logo;
                ApplicationSettings.ProfileAvatar = model.Avatar;
                BlogSettings.Cover = model.Image;
                BlogSettings.Theme = model.BlogTheme;

                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.Title,
                    model.Title);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.Description,
                    model.Description);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.ProfileLogo,
                    model.Logo);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.ProfileAvatar,
                    model.Avatar);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.ProfileImage,
                    model.Image);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.BlogTheme,
                    model.BlogTheme);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.SendGridApiKey,
                    model.EmailKey);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.HeadCode,
                    model.BlogHead);
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.FooterCode,
                    model.BlogFooter);

                model.Profile.BlogTheme = model.BlogTheme;

                this.db.Complete();

                ViewBag.Message = "Updated";
            }

            return View(this.theme + "General.cshtml",
                model);
        }

        [MustBeAdmin]
        [Route("posts")]
        public IActionResult Posts()
        {
            var profile = GetProfile();

            var model = new SettingsPosts
            {
                Profile = profile,
                PostImage = BlogSettings.Cover,
                PostFooter = this.db.CustomFields.GetValue(CustomType.Application,
                    0,
                    Constants.PostCode),
                ItemsPerPage = BlogSettings.ItemsPerPage
            };
            return View(this.theme + "Posts.cshtml",
                model);
        }

        [HttpPost]
        [MustBeAdmin]
        [Route("posts")]
        public IActionResult Posts(SettingsPosts model)
        {
            model.Profile = GetProfile();

            if (ModelState.IsValid)
            {
                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.ItemsPerPage,
                    model.ItemsPerPage.ToString());
                BlogSettings.ItemsPerPage = model.ItemsPerPage;

                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.PostImage,
                    model.PostImage);
                BlogSettings.PostCover = model.PostImage;

                this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.PostCode,
                    model.PostFooter);

                this.db.Complete();

                ViewBag.Message = "Updated";
            }

            return View(this.theme + "Posts.cshtml",
                model);
        }

        [MustBeAdmin]
        [Route("advanced")]
        public IActionResult Advanced()
        {
            var profile = GetProfile();

            var model = new SettingsAdvanced
            {
                Profile = profile
            };
            return View(this.theme + "Advanced.cshtml",
                model);
        }

        private Profile GetProfile()
        {
            return this.db.Profiles.Single(p => p.IdentityName == User.Identity.Name);
        }

        private void SaveCustomFields(Dictionary<string, string> fields, int profileId)
        {
            if (fields != null && fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    this.db.CustomFields.SetCustomField(CustomType.Profile,
                        profileId,
                        field.Key,
                        field.Value);
                }
            }
        }

        private string SlugFromTitle(string title)
        {
            var slug = title.ToSlug();
            if (this.db.Profiles.Single(b => b.Slug == slug) == null)
            {
                return slug;
            }

            {
                for (var i = 2; i < 100; i++)
                {
                    var i1 = i;
                    if (this.db.Profiles.Single(b => b.Slug == slug + i1.ToString()) == null)
                    {
                        return slug + i;
                    }
                }
            }

            return slug;
        }
    }
}