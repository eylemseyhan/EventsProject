using Microsoft.AspNetCore.Mvc;

namespace EventsProject.ViewComponents
{
    public class _UILayoutAllEventsComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
