﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EntityLayer.Concrete;
using EventsProject.Areas.Member.Models;
using Microsoft.AspNetCore.Authorization;
using DataAccessLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Abstract;
using EventsProject.Controllers;
using System.Globalization;
namespace EventsProject.Areas.Member.Controllers
{
    [Area("Member")]
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class DashboardController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        
        private readonly IEventService _eventService;

        Context db = new Context();

        public DashboardController(UserManager<AppUser> userManager, IEventService eventService)
        {
            _userManager = userManager;
            _eventService = eventService;

        }

        // Dashboard ana sayfası
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("SignIn", "Login", new { area = "Member" });
            }

            var model = new DashboardViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Image = user.Image ?? "/images/profiles/icons8-user-50.png" // Varsayılan profil resmi
            };

            return View(model);
        }

        // Profil sayfası
        public IActionResult Profile()
        {
            // Profil sayfası için işlem yapılacak
            return View();
        }

        public IActionResult Details(int id)
        {
            var eventDetails = _eventService.TGetByID(id);
            if (eventDetails == null)
            {
                return NotFound();
            }

            return View(eventDetails); // Etkinlik detayları için view'a gönderiyoruz
        }

        public async Task<IActionResult> Tickets()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("SignIn", "Login", new { area = "Member" });
            }

            // Kullanıcının biletlerini ve ilişkili etkinlikleri manuel bir join ile alıyoruz
            var ticketsWithEvents = await (from t in db.Tickets
                                           join et in db.EventsTickets on t.EventsTicketId equals et.EventsTicketId
                                           join e in db.Events on et.EventId equals e.EventId
                                           where t.UserId == user.Id
                                           select new TicketEventViewModel
                                           {
                                               TicketId = t.TicketId,
                                               TicketNumber = t.TicketNumber,
                                               IsAvailable = t.IsAvailable,
                                               EventId = e.EventId,
                                               EventName = e.Title,
                                               EventCategory = e.Category.Name,
                                               EventArtist = e.Artist.Name,
                                               EventDate = e.EventDate,
                                               EventLocation = e.Location,
                                               ImageUrl = e.ImageUrl,
                                           }).ToListAsync();

            return View(ticketsWithEvents);
        }





        // Kullanıcı favoriler sayfası

        public async Task<IActionResult> Favorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Login", new { area = "Member" });
            }

            var favoriteEvents = await (from uf in db.UserFavorites
                                        join e in db.Events on uf.EventId equals e.EventId
                                        where uf.UserId == user.Id
                                        select new
                                        {
                                            Event = e,
                                            Category = e.Category,
                                            Artist = e.Artist
                                        }).ToListAsync();


            var events = favoriteEvents.Select(uf => uf.Event).ToList();

            return View(events); // Favori etkinlikleri view'a gönderiyoruz
        }

        // Kullanıcı ayarları sayfası
        public async Task<IActionResult> Settings()
        {
            // Kullanıcıyı al
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Kullanıcı oturum açmamışsa login sayfasına yönlendir
                return RedirectToAction("Login", "Account");
            }

            // Kullanıcı modelini View'e gönder
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string currentPassword, string newPassword, string email)
        {
            // Mevcut kullanıcının bilgilerini al
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Şifre değiştirme işlemi
            if (!string.IsNullOrEmpty(newPassword))
            {
                var passwordCheck = await _userManager.CheckPasswordAsync(user, currentPassword);
                if (passwordCheck)
                {
                    var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View("Settings", user);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Mevcut şifre yanlış.");
                    return View("Settings", user);
                }
            }

            // E-posta güncelleme işlemi
            if (!string.IsNullOrEmpty(email) && email != user.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, email);
                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View("Settings", user);
                }
            }

            // Başarıyla güncellendi
            TempData["SuccessMessage"] = "Bilgileriniz başarıyla güncellendi.";
            return RedirectToAction("Settings");
        }

      


        [HttpPost]
        public async Task<IActionResult> UpdatePersonalInfo(string Name, string Surname, string Email)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.Name = Name;
            user.Surname = Surname;
            user.Email = Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("Settings", user);
            }

            TempData["SuccessMessage"] = "Bilgiler başarıyla güncellendi.";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Yeni şifreler eşleşmiyor.");
                return View("Settings", user);
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Şifre değiştirilemedi. Lütfen mevcut şifrenizi doğru girdiğinizden emin olun.");
                return View("Settings", user);
            }

            return RedirectToAction("Settings");
        }
        [HttpPost]
        public IActionResult UpdateNotifications(bool EmailNotifications, bool SmsNotifications, bool PushNotifications)
        {
            // Kullanıcının bildirim ayarlarını veritabanında güncelleyin (örnek kod)
            // NotificationSettingsService.Update(user.Id, EmailNotifications, SmsNotifications, PushNotifications);

            return RedirectToAction("Settings");
        }

        
        public async Task<IActionResult> SavedCards()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("SignIn", "Login", new { area = "Member" });
            }

            var savedCards = db.SavedCards.Where(sc => sc.UserId == user.Id).ToList();
            return View(savedCards);
        }
        [HttpPost]
        public async Task<IActionResult> AddSavedCard(string cardHolderName, string cardNumber, string expiryDate, string cvv)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, redirectTo = Url.Action("SignIn", "Login", new { area = "Member" }) });
            }

            DateTime expiryDateTime;
            // Tarihi "MM/yy" formatında parse et
            if (DateTime.TryParseExact(expiryDate, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out expiryDateTime))
            {
                // "MM/YY" formatında yıl ve ay alındı, ancak gün ve saat bilgisi yok.
                // Bu nedenle, tarih nesnesine 1. günü ve saat 00:00 ekliyoruz
                expiryDateTime = new DateTime(expiryDateTime.Year, expiryDateTime.Month, 1, 0, 0, 0);

                // Zaman dilimini UTC olarak ayarlıyoruz
                if (expiryDateTime.Kind == DateTimeKind.Unspecified)
                {
                    expiryDateTime = DateTime.SpecifyKind(expiryDateTime, DateTimeKind.Utc);
                }

                // SavedCard oluşturuluyor
                var savedCard = new SavedCard
                {
                    CardHolderName = cardHolderName,
                    CardNumber = cardNumber.Replace(" ", ""), // Boşlukları temizle
                    ExpiryDate = expiryDateTime, // UTC zamanı kaydediyoruz
                    CVV = cvv,
                    UserId = user.Id
                };

                db.SavedCards.Add(savedCard);
                await db.SaveChangesAsync();
            }
            else
            {
                return Json(new { success = false, message = "Geçerli bir son kullanma tarihi girin." });
            }

            // Yönlendirme işlemi burada yapılır
            return RedirectToAction("SavedCards", "Dashboard", new { area = "Member" });
        }












    }
}
