namespace Blogifier.Core.Controllers.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using Data.Domain;
    using Data.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    [Route("blogifier/api/[controller]")]
    public class PackagesController : Controller
    {
        private readonly IUnitOfWork db;
        private ILogger logger;

        public PackagesController(IUnitOfWork db, ILogger<AssetsController> logger)
        {
            this.db = db;
            this.logger = logger;
        }

        [HttpPut("enable/{id}")]
        public async Task Enable(string id)
        {
            var disabled = Disabled();
            if (disabled != null && disabled.Contains(id))
            {
                disabled.Remove(id);
                await this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.DisabledPackages,
                    string.Join(",",
                        disabled));
            }
        }

        [HttpPut("disable/{id}")]
        public async Task Disable(string id)
        {
            var disabled = Disabled();
            if (disabled == null)
            {
                await this.db.CustomFields.SetCustomField(CustomType.Application,
                    0,
                    Constants.DisabledPackages,
                    id);
            }
            else
            {
                if (!disabled.Contains(id))
                {
                    disabled.Add(id);
                    await this.db.CustomFields.SetCustomField(CustomType.Application,
                        0,
                        Constants.DisabledPackages,
                        string.Join(",",
                            disabled));
                }
            }
        }

        private List<string> Disabled()
        {
            var field = this.db.CustomFields.GetValue(CustomType.Application,
                0,
                Constants.DisabledPackages);
            return string.IsNullOrEmpty(field) ? null : field.Split(',').ToList();
        }

        private Profile GetProfile()
        {
            try
            {
                return this.db.Profiles.Single(p => p.IdentityName == User.Identity.Name);
            }
            catch
            {
                RedirectToAction("Login",
                    "Account");
            }

            return null;
        }
    }
}