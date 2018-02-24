using Blogifier.Core.Common;
using Blogifier.Core.Services.Data;
using Blogifier.Core.Services.Syndication.Rss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blogifier.Core.Controllers
{
    public class BlogController : Controller
	{
        IRssService rss;
        IDataService ds;
        private readonly ILogger logger;
        private readonly string theme;

		public BlogController(IRssService rss, IDataService ds, ILogger<BlogController> logger)
		{
            this.rss = rss;
            this.ds = ds;
            this.logger = logger;
            this.theme = $"~/{ApplicationSettings.BlogThemesFolder}/{BlogSettings.Theme}/";
        }

        public IActionResult Index(int page = 1)
        {
            var model = this.ds.GetPosts(page);
            if (model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View(this.theme + "Index.cshtml", model);
        }

        [Route("{slug:author}")]
        public IActionResult PostsByAuthor(string slug, int page = 1)
        {
            var model = this.ds.GetPostsByAuthor(slug, page);
            if(model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View($"~/{ApplicationSettings.BlogThemesFolder}/" + model.Profile.BlogTheme + "/Author.cshtml", model);
        }

        [Route("category/{cat}")]
        public IActionResult AllPostsByCategory(string cat, int page = 1)
        {
            var model = this.ds.GetAllPostsByCategory(cat, page);
            if (model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View($"~/{ApplicationSettings.BlogThemesFolder}/{BlogSettings.Theme}/Category.cshtml", model);
        }

        [Route("{slug:author}/{cat}")]
        public IActionResult PostsByCategory(string slug, string cat, int page = 1)
        {
            var model = this.ds.GetPostsByCategory(slug, cat, page);
            if(model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View($"~/{ApplicationSettings.BlogThemesFolder}/" + model.Profile.BlogTheme + "/Category.cshtml", model);
        }

        [Route("{slug}")]
        public IActionResult SinglePublication(string slug)
        {
            var model = this.ds.GetPostBySlug(slug);
            if (model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View($"~/{ApplicationSettings.BlogThemesFolder}/" + model.Profile.BlogTheme + "/Single.cshtml", model);
        }

        [Route("search/{term}")]
        public IActionResult PagedSearch(string term, int page = 1)
        {
            ViewBag.Term = term;
            var model = this.ds.SearchPosts(term, page);

            if (model == null)
                return View(this.theme + "Error.cshtml", 404);

            return View(this.theme + "Search.cshtml", model);
        }

        [HttpPost]
        public IActionResult Search()
        {
            ViewBag.Term = HttpContext.Request.Form["term"];
            var model = this.ds.SearchPosts(ViewBag.Term, 1);

            return View(this.theme + "Search.cshtml", model);
        }

        [Route("rss/{slug:author?}")]
        public IActionResult Rss(string slug)
        {
            var absoluteUri = string.Concat(
                Request.Scheme, "://",
                Request.Host.ToUriComponent(),
                Request.PathBase.ToUriComponent());

            var x = slug;

            var rss = this.rss.Display(absoluteUri, slug);
            return Content(rss, "text/xml");
        }

        [Route("error/{statusCode}")]
        public IActionResult Error(int statusCode)
        {
            return View(this.theme + "Error.cshtml", statusCode);
        }
    }
}