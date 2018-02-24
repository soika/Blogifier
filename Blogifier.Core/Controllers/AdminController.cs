namespace Blogifier.Core.Controllers
{
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
    using Services.Search;

    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly string theme;

        public AdminController(
            IUnitOfWork unitOfWork,
            ISearchService search,
            ILogger<AdminController> logger)
        {
            this.unitOfWork = unitOfWork;
            this.theme = $"~/{ApplicationSettings.BlogAdminFolder}/";
        }

        [VerifyProfile]
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index",
                "Content");
        }

        [VerifyProfile]
        [Route("files")]
        public IActionResult Files(string search = "")
        {
            return View(this.theme + "Files.cshtml",
                new AdminBaseModel {Profile = GetProfile()});
        }

        [Route("setup")]
        public IActionResult Setup()
        {
            return View(this.theme + "Setup.cshtml",
                new AdminSetupModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("setup")]
        public IActionResult Setup(AdminSetupModel model)
        {
            if (ModelState.IsValid)
            {
                var profile = new Profile();

                if (this.unitOfWork.Profiles.All().ToList().Count == 0)
                {
                    profile.IsAdmin = true;
                }

                profile.AuthorName = model.AuthorName;
                profile.AuthorEmail = model.AuthorEmail;
                profile.Title = model.Title;
                profile.Description = model.Description;

                profile.IdentityName = User.Identity.Name;
                profile.Slug = SlugFromTitle(profile.AuthorName);
                profile.Avatar = ApplicationSettings.ProfileAvatar;
                profile.BlogTheme = BlogSettings.Theme;

                profile.LastUpdated = SystemClock.Now();

                this.unitOfWork.Profiles.Add(profile);
                this.unitOfWork.Complete();

                return RedirectToAction("Index");
            }

            return View(this.theme + "Setup.cshtml",
                model);
        }

        private Profile GetProfile()
        {
            return this.unitOfWork.Profiles.Single(b => b.IdentityName == User.Identity.Name);
        }

        private string SlugFromTitle(string title)
        {
            var slug = title.ToSlug();
            if (this.unitOfWork.Profiles.Single(b => b.Slug == slug) == null)
            {
                return slug;
            }

            {
                for (var i = 2; i < 100; i++)
                {
                    var i1 = i;

                    if (this.unitOfWork.Profiles.Single(b => b.Slug == slug + i1.ToString()) == null)
                    {
                        return slug + i;
                    }
                }
            }

            return slug;
        }
    }
}