namespace Blogifier.Core.Controllers.Api.Public
{
    using Data.Models;
    using Microsoft.AspNetCore.Mvc;
    using Services.Data;

    [Route("blogifier/api/public/[controller]")]
    public class PostsController : Controller
    {
        private readonly IDataService dataService;

        public PostsController(IDataService dataService)
        {
            this.dataService = dataService;
        }

        // GET blogifier/api/public/posts
        // GET blogifier/api/public/posts?page=2
        public BlogPostsModel Get(int page = 1)
        {
            return this.dataService.GetPosts(page,
                true);
        }

        // GET blogifier/api/public/posts/author/filip-stanek
        // GET blogifier/api/public/posts/author/filip-stanek?page=2
        [HttpGet("[action]/{slug}")]
        public BlogAuthorModel Author(string slug, int page = 1)
        {
            return this.dataService.GetPostsByAuthor(slug,
                page,
                true);
        }

        // GET blogifier/api/public/posts/author/category/mobile
        // GET blogifier/api/public/posts/author/category/mobile?page=2
        [HttpGet("[action]/{auth}/{cat}")]
        public BlogCategoryModel Category(string auth, string cat, int page = 1)
        {
            return this.dataService.GetPostsByCategory(auth,
                cat,
                page,
                true);
        }

        // GET blogifier/api/public/posts/search/dot%20net
        // GET blogifier/api/public/posts/search/dot%20net?page=2
        [HttpGet("[action]/{term}")]
        public BlogPostsModel Search(string term, int page = 1)
        {
            return this.dataService.SearchPosts(term,
                page,
                true);
        }

        // GET blogifier/api/public/posts/post/running-local-web-pages-in-cefsharpwpf
        [HttpGet("[action]/{slug}")]
        public BlogPostDetailModel Post(string slug)
        {
            return this.dataService.GetPostBySlug(slug,
                true);
        }
    }
}