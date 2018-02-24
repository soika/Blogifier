namespace Blogifier.Core.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common;
    using Data.Domain;
    using Data.Interfaces;
    using Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Middleware;
    using Services.Packages;

    [Authorize]
    [Route("admin/[controller]")]
    public class PackagesController : Controller
    {
        private readonly string theme;
        private readonly IUnitOfWork db;
        private readonly IPackageService pkgs;

        public PackagesController(IUnitOfWork db, IPackageService pkgs)
        {
            this.db = db;
            this.pkgs = pkgs;
            this.theme = $"~/{ApplicationSettings.BlogAdminFolder}/";
        }

        [VerifyProfile]
        [HttpGet("widgets")]
        public async Task<IActionResult> Widgets()
        {
            var model = new AdminPackagesModel
            {
                Profile = GetProfile(),
                Packages = await this.pkgs.Find(PackageType.Widgets)
            };
            return View($"{this.theme}Packages/Widgets.cshtml",
                model);
        }

        [VerifyProfile]
        [HttpGet("themes")]
        public IActionResult Themes()
        {
            var model = new AdminPackagesModel {Profile = GetProfile()};
            model.Packages = new List<PackageListItem>();

            return View($"{this.theme}Packages/Themes.cshtml",
                model);
        }

        private Profile GetProfile()
        {
            return this.db.Profiles.Single(b => b.IdentityName == User.Identity.Name);
        }
    }
}