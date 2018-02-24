using Blogifier.Core.Common;
using Blogifier.Core.Data.Domain;
using Blogifier.Core.Data.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Blogifier.Core.Data.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        BlogifierDbContext db;

        public CategoryRepository(BlogifierDbContext db) : base(db)
        {
            this.db = db;
        }

        public IEnumerable<Category> Find(Expression<Func<Category, bool>> predicate, Pager pager)
        {
            if(pager == null)
            {
                return this.db.Categories.AsNoTracking()
                    .Include(c => c.PostCategories)
                    .Where(predicate)
                    .OrderBy(c => c.Title);
            }

            var skip = pager.CurrentPage * pager.ItemsPerPage - pager.ItemsPerPage;

            var categories = this.db.Categories.AsNoTracking()
                .Include(c => c.PostCategories)
                .Where(predicate)
                .OrderBy(c => c.Title)
                .ToList();

            pager.Configure(categories.Count());
            
            return categories.Skip(skip).Take(pager.ItemsPerPage);
        }

        public IEnumerable<SelectListItem> PostCategories(int postId)
        {
            var items = new List<SelectListItem>();
            var postCategories = this.db.PostCategories.Include(pc => pc.Category).Where(c => c.BlogPostId == postId);
            foreach (var item in postCategories)
            {
                var newItem = new SelectListItem { Value = item.Id.ToString(), Text = item.Category.Title };
                if (!items.Contains(newItem))
                {
                    items.Add(newItem);
                }
            }
            return items.OrderBy(c => c.Text);
        }

        public IEnumerable<SelectListItem> CategoryList(Expression<Func<Category, bool>> predicate)
        {
            return this.db.Categories.Where(predicate).OrderBy(c => c.Title)
                .Select(c => new SelectListItem { Text = c.Title, Value = c.Id.ToString() }).ToList();
        }

        public async Task<Category> SingleIncluded(Expression<Func<Category, bool>> predicate)
        {
            return await this.db.Categories.AsNoTracking()
                .Include(c => c.PostCategories)
                .FirstOrDefaultAsync(predicate);
        }

        public bool AddCategoryToPost(int postId, int categoryId)
        {
            try
            {
                var existing = this.db.PostCategories.Where(
                    pc => pc.BlogPostId == postId &&
                    pc.CategoryId == categoryId).SingleOrDefault();

                if (existing == null)
                {
                    this.db.PostCategories.Add(new PostCategory
                    {
                        BlogPostId = postId,
                        CategoryId = categoryId,
                        LastUpdated = SystemClock.Now()
                    });
                    this.db.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveCategoryFromPost(int postId, int categoryId)
        {
            try
            {
                var existing = this.db.PostCategories.Where(
                    pc => pc.BlogPostId == postId &&
                    pc.CategoryId == categoryId).SingleOrDefault();

                if (existing == null)
                {
                    return false;
                }

                this.db.PostCategories.Remove(existing);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}