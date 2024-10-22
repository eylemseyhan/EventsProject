using Microsoft.AspNetCore.Mvc;

namespace EventsProject.ViewComponents
{
    public class _UILayoutUpComingEventsComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
