using System;
using Microsoft.AspNetCore.Mvc;
namespace EventsProject.ViewComponents
{
    public class _UILayoutSearchComponent:ViewComponent

    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
