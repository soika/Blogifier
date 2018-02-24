namespace Blogifier.Core.Controllers.Api
{
    using System;
    using System.Collections.Generic;
    using Common;
    using Data.Domain;
    using Data.Interfaces;
    using Data.Models;
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;

    [Authorize]
    [Route("blogifier/api/[controller]")]
    public class CategoriesController : Controller
    {
        private readonly IMemoryCache cache;
        private readonly IUnitOfWork db;

        public CategoriesController(IUnitOfWork db, IMemoryCache memoryCache)
        {
            this.db = db;
            this.cache = memoryCache;
        }

        [HttpGet("blogcategories")]
        public IEnumerable<string> GetBlogCategories()
        {
            var blogCats = new List<string>();
            var cats = this.db.Categories.Find(c => c.ProfileId == GetProfile().Id);
            foreach (var cat in cats)
            {
                blogCats.Add(cat.Title);
            }

            return blogCats;
        }

        [HttpGet]
        public IEnumerable<CategoryItem> Get(int page)
        {
            return CategoryList.Items(this.db,
                GetProfile().Id);
        }

        [HttpGet("{slug}")]
        public IEnumerable<CategoryItem> GetBySlug(string slug)
        {
            var post = this.db.BlogPosts.Single(p => p.Slug == slug);
            var postId = post == null ? 0 : post.Id;

            return CategoryList.Items(this.db,
                GetProfile().Id,
                postId);
        }

        [HttpGet("{id:int}")]
        public CategoryItem GetById(int id)
        {
            return GetItem(this.db.Categories.Single(c => c.Id == id));
        }

        [HttpPost("addcategory")]
        public IActionResult AddCategory([FromBody] AdminCategoryModel model)
        {
            var profile = GetProfile();
            var existing = this.db.Categories.Single(c => c.Title == model.Title && c.ProfileId == profile.Id);
            if (existing == null)
            {
                var newCategory = new Category
                {
                    ProfileId = profile.Id,
                    Title = model.Title,
                    Description = model.Title,
                    Slug = model.Title.ToSlug(),
                    LastUpdated = SystemClock.Now()
                };

                this.db.Categories.Add(newCategory);
                this.db.Complete();

                existing = this.db.Categories.Single(c => c.Title == model.Title && c.ProfileId == profile.Id);
            }

            var callback = new {existing.Id, existing.Title};
            return new CreatedResult("blogifier/api/categories/" + existing.Id,
                callback);
        }

        [HttpPut("addcategorytopost")]
        public void AddCategoryToPost([FromBody] AdminCategoryModel model)
        {
            var existing = this.db.Categories.Single(c => c.Title == model.Title);
            if (existing == null)
            {
                var newCategory = new Category
                {
                    ProfileId = GetProfile().Id,
                    Title = model.Title,
                    Description = model.Title,
                    Slug = model.Title.ToSlug(),
                    LastUpdated = SystemClock.Now()
                };
                this.db.Categories.Add(newCategory);
                this.db.Complete();

                existing = this.db.Categories.Single(c => c.Title == model.Title);
            }

            this.db.Categories.AddCategoryToPost(int.Parse(model.PostId),
                existing.Id);
            this.db.Complete();
        }

        [HttpPut("removecategoryfrompost")]
        public void RemoveCategoryFromPost([FromBody] AdminCategoryModel model)
        {
            this.db.Categories.RemoveCategoryFromPost(int.Parse(model.PostId),
                int.Parse(model.CategoryId));
            this.db.Complete();
        }

        [HttpPut]
        public IActionResult Put([FromBody] CategoryItem category)
        {
            var blog = GetProfile();
            if (ModelState.IsValid)
            {
                var id = string.IsNullOrEmpty(category.Id) ? 0 : int.Parse(category.Id);
                if (id > 0)
                {
                    var existing = this.db.Categories.Single(c => c.Id == id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Title = category.Title;
                    existing.Description = string.IsNullOrEmpty(category.Description)
                        ? category.Title
                        : category.Description;
                    existing.LastUpdated = SystemClock.Now();
                    this.db.Complete();
                }
                else
                {
                    var newCategory = new Category
                    {
                        ProfileId = blog.Id,
                        Title = category.Title,
                        Description =
                            string.IsNullOrEmpty(category.Description) ? category.Title : category.Description,
                        Slug = category.Title.ToSlug(),
                        LastUpdated = SystemClock.Now()
                    };
                    this.db.Categories.Add(newCategory);
                    this.db.Complete();
                }
            }

            return new NoContentResult();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var category = this.db.Categories.Single(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            this.db.Categories.Remove(category);
            this.db.Complete();
            return new NoContentResult();
        }

        private CategoryItem GetItem(Category category)
        {
            var vCount = 0;
            var pCount = 0;
            if (category.PostCategories != null && category.PostCategories.Count > 0)
            {
                pCount = category.PostCategories.Count;
                foreach (var pc in category.PostCategories)
                {
                    vCount += this.db.BlogPosts.Single(p => p.Id == pc.BlogPostId).PostViews;
                }

                this.db.Complete();
            }

            return new CategoryItem
            {
                Id = category.Id.ToString(),
                Title = category.Title,
                Description = category.Description,
                Selected = false,
                PostCount = pCount,
                ViewCount = vCount
            };
        }

        private Profile GetProfile()
        {
            var key = "_BLOGIFIER_CACHE_BLOG_KEY";
            Profile publisher;
            if (this.cache.TryGetValue(key,
                out publisher))
            {
                return publisher;
            }

            publisher = this.db.Profiles.Single(b => b.IdentityName == User.Identity.Name);
            this.cache.Set(key,
                publisher,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(2)));
            return publisher;
        }
    }
}