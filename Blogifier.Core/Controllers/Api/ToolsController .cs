using Blogifier.Core.Data.Domain;
using Blogifier.Core.Data.Interfaces;
using Blogifier.Core.Data.Models;
using Blogifier.Core.Services.FileSystem;
using Blogifier.Core.Services.Syndication.Rss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blogifier.Core.Controllers.Api
{
    [Authorize]
    [Route("blogifier/api/[controller]")]
    public class ToolsController : Controller
    {
        IUnitOfWork db;
        IRssService rss;
        private readonly ILogger logger;

        public ToolsController(IUnitOfWork db, IRssService rss, ILogger<AdminController> logger)
        {
            this.db = db;
            this.rss = rss;
            this.logger = logger;
        }

        // PUT: api/tools/rssimport
        [HttpPut]
        [Route("rssimport")]
        public async Task<HttpResponseMessage> RssImport([FromBody]RssImportModel rss)
        {
            var profile = GetProfile();
            rss.ProfileId = profile.Id;
            rss.Root = Url.Content("~/");
            
            return await this.rss.Import(rss);
        }

        [HttpDelete("{id}")]
        [Route("deleteblog/{id}")]
        public IActionResult Delete(int id)
        {
            var profile = GetProfile();

            if (!profile.IsAdmin || profile.Id == id)
                return NotFound();

            this.logger.LogInformation(string.Format("Delete blog {0} by {1}", id, profile.AuthorName));

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

            return new NoContentResult();
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