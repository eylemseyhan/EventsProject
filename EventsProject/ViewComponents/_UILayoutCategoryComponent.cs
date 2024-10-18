using System;
using Microsoft.AspNetCore.Mvc;


namespace EventsProject.ViewComponents
{
    public class _UILayoutCategoryComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
