using Microsoft.AspNetCore.Mvc;

namespace EventsProject.ViewComponents
{
    public class _UILayoutPopulerEventsComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
