using Blogifier.Core.Data.Domain;
using Blogifier.Core.Data.Interfaces;
using Blogifier.Core.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Blogifier.Core.Controllers.Api
{
    [Authorize]
    [Route("blogifier/api/[controller]")]
    public class ProfileController : Controller
    {
        IUnitOfWork db;

        public ProfileController(IUnitOfWork db)
        {
            this.db = db;
        }

        // PUT: api/profile/setcustomfield
        [HttpPut]
        [Route("setcustomfield")]
        public async Task SetCustomField([FromBody]CustomFieldItem item)
        {
            var profile = GetProfile();
            await this.db.CustomFields.SetCustomField(CustomType.Profile, profile.Id, item.CustomKey, item.CustomValue);
        }

        Profile GetProfile()
        {
            try
            {
                return this.db.Profiles.Single(p => p.IdentityName == User.Identity.Name);
            }
            catch
            {
                RedirectToAction("Login", "Account");
            }
            return null;
        }
    }
}