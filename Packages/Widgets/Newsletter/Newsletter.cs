using Blogifier.Core.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Blogifier.Widgets
{
    [ViewComponent(Name = "Newsletter")]
    public class Newsletter : ViewComponent
    {
        IUnitOfWork db;

        public Newsletter(IUnitOfWork db)
        {
            this.db = db;
        }

        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}