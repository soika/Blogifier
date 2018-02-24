namespace Blogifier.Core.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using Data.Domain;
    using Data.Interfaces;
    using Data.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Middleware;
    using Services.Search;

    [Authorize]
    [Route("admin/[controller]")]
    public class ContentController : Controller
    {
        private readonly string theme;
        private readonly IUnitOfWork db;
        private readonly ISearchService search;

        public ContentController(IUnitOfWork db, ISearchService search)
        {
            this.db = db;
            this.search = search;
            this.theme = $"~/{ApplicationSettings.BlogAdminFolder}/Content/";
        }

        [VerifyProfile]
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string user = "0", string status = "A", string cats = "",
            string search = "")
        {
            var profile = GetProfile();

            var fields = await this.db.CustomFields.GetCustomFields(CustomType.Profile,
                profile.Id);
            var pageSize = BlogSettings.ItemsPerPage;

            if (fields.ContainsKey(Constants.PostListSize))
            {
                pageSize = int.Parse(fields[Constants.PostListSize]);
            }

            var pager = new Pager(page,
                pageSize);
            var model = new AdminPostsModel {Profile = profile};

            model.CustomFields = fields;

            if (model.Profile.IsAdmin)
            {
                model.Users = this.db.Profiles.Find(p => p.IdentityName != model.Profile.IdentityName);
            }

            var userProfile = model.Profile;
            if (user != "0" && profile.IsAdmin)
            {
                userProfile = this.db.Profiles.Single(p => p.Id == int.Parse(user));
            }

            model.StatusFilter = GetStatusFilter(status);

            var selectedCategories = new List<string>();
            var dbCategories = new List<Category>();
            model.CategoryFilter = this.db.Categories.CategoryList(c => c.ProfileId == userProfile.Id).ToList();
            if (!string.IsNullOrEmpty(cats))
            {
                selectedCategories = cats.Split(',').ToList();
                foreach (var ftr in model.CategoryFilter)
                {
                    if (selectedCategories.Contains(ftr.Value))
                    {
                        ftr.Selected = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(search))
            {
                model.BlogPosts = this.db.BlogPosts.ByFilter(status,
                    selectedCategories,
                    userProfile.Slug,
                    pager).Result;
            }
            else
            {
                model.BlogPosts = this.search.Find(pager,
                    search,
                    userProfile.Slug).Result;
            }

            model.Pager = pager;

            var anyPost = this.db.BlogPosts.Find(p => p.ProfileId == userProfile.Id).FirstOrDefault();
            ViewBag.IsFirstPost = anyPost == null;

            return View(this.theme + "Index.cshtml",
                model);
        }

        [VerifyProfile]
        [Route("editor/{id:int}")]
        public IActionResult Editor(int id, string user = "0")
        {
            var profile = GetProfile();
            var userProfile = profile;

            if (user != "0")
            {
                userProfile = this.db.Profiles.Single(p => p.Id == int.Parse(user));
            }

            var post = new BlogPost();
            var categories = this.db.Categories.CategoryList(c => c.ProfileId == userProfile.Id).ToList();

            if (id > 0)
            {
                if (profile.IsAdmin)
                {
                    post = this.db.BlogPosts.SingleIncluded(p => p.Id == id).Result;
                }
                else
                {
                    post = this.db.BlogPosts.SingleIncluded(p => p.Id == id && p.Profile.Id == profile.Id).Result;
                }
            }

            if (post.PostCategories != null)
            {
                foreach (var pc in post.PostCategories)
                {
                    foreach (var cat in categories)
                    {
                        if (pc.CategoryId.ToString() == cat.Value)
                        {
                            cat.Selected = true;
                        }
                    }
                }
            }

            var model = new AdminEditorModel {Profile = profile, CategoryList = categories, BlogPost = post};
            return View(this.theme + "Editor.cshtml",
                model);
        }

        private Profile GetProfile()
        {
            return this.db.Profiles.Single(b => b.IdentityName == User.Identity.Name);
        }

        private List<SelectListItem> GetStatusFilter(string filter)
        {
            return new List<SelectListItem>
            {
                new SelectListItem {Text = "All", Value = "A", Selected = filter == "A"},
                new SelectListItem {Text = "Drafts", Value = "D", Selected = filter == "D"},
                new SelectListItem {Text = "Published", Value = "P", Selected = filter == "P"}
            };
        }
    }
}