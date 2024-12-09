﻿using DataAccessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventsProject.Controllers
{
    public class TheaterController : Controller
    {
        Context db = new Context();  // DbContext'i başlatıyoruz

        public IActionResult Index()
        {

            var theaters = db.Events.Where(e => e.CategoryId == 2).ToList();
            return View(theaters);
        }

        public IActionResult Details(int id)
        {
            var eventDetail = db.Events.FirstOrDefault(x => x.EventId == id);
            if (eventDetail == null)
            {
                return NotFound(); // Etkinlik bulunamazsa 404 döndür
            }
            return View(eventDetail); // Bu view bir Event nesnesi bekliyor
        }


        [HttpPost]
        public IActionResult AddToFavorites(int? eventId) // Nullable kontrolü ekleyelim
        {
            if (eventId == null || eventId == 0)
            {
                return Json(new { success = false, message = "Geçersiz EventId" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // EventId'nin veritabanında var olup olmadığını kontrol edin
            var eventExists = db.Events.Any(x => x.EventId == eventId.Value);
            if (!eventExists)
            {
                return Json(new { success = false, message = "Etkinlik bulunamadı." });
            }

            var userFavorite = new UserFavorite
            {
                UserId = userId,
                EventId = eventId.Value
            };

            db.UserFavorites.Add(userFavorite);
            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}
