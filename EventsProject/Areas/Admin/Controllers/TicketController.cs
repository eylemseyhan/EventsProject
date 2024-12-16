﻿using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using EventsProject.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly IEventsTicketsService _eventsTicketService;
        private readonly IEventService _eventService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(IEventsTicketsService eventsTicketService, IEventService eventService, ILogger<TicketController> logger, ITicketService ticketService)
        {
            _eventsTicketService = eventsTicketService;
            _ticketService = ticketService;
            _eventService = eventService;
            _logger = logger;
        }

        // Event biletlerini listeleme
        public IActionResult Index()
        {
            var tickets = _eventsTicketService.TGetList() ?? new List<EventsTickets>();

            foreach (var ticket in tickets)
            {
                var eventTitle = _eventService.TGetByID(ticket.EventId)?.Title;
                ticket.EventTitle = eventTitle; // Görselde kullanmak için ekliyoruz
            }
            


            return View(tickets);
        }


       
     


        [HttpGet]
        public IActionResult Create()
        {
            var model = new EventTicketCreateDto(); // Model oluşturuluyor
            var events = _eventService.GetAllEvents();

            // Etkinlikleri kontrol et ve ViewBag'a atayın
            if (events != null && events.Any())
            {
                ViewBag.Events = new SelectList(events, "EventId", "Title");
            }
            else
            {
                _logger.LogWarning("Etkinlikler listesi boş.");
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult Create(EventTicketCreateDto model)
        {
            var events = _eventService.GetAllEvents();
            if (events != null && events.Any())
            {
                ViewBag.Events = new SelectList(events, "EventId", "Title");
            }

            // ModelState kontrolü
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                foreach (var error in errors)
                {
                    _logger.LogWarning(error);
                }
                return View(model); // Hatalar varsa formu geri gönder
            }

            try
            {
                var associatedEvent = _eventService.TGetByID(model.EventId);
                if (associatedEvent == null)
                {
                    ModelState.AddModelError("EventId", "Seçilen etkinlik bulunamadı.");
                    return View(model);
                }

                // Yeni EventsTicket nesnesi oluştur
                var eventsTicket = new EventsTickets
                {
                    EventId = model.EventId,
                    TicketCapacity = model.TicketCapacity,
                    Price = model.Price,
                    Name = model.Name,
                    SoldCount = 0,
                };

                _eventsTicketService.TAdd(eventsTicket);  // EventsTicket ekle

                var newEventsTicketId = eventsTicket.EventsTicketId;

                // EventId'nin EventsTicket ile ilişkili olduğunu varsayıyoruz
                var eventId = eventsTicket.EventId;  // eventsTicket ile ilişkilendirilmiş EventId'yi alıyoruz

                // Biletleri ekle
                for (int i = 0; i < model.TicketCapacity; i++)
                {
                    // Ticket oluşturma modelini kullan
                    var ticketModel = new TicketCreateDto
                    {
                        EventsTicketId = newEventsTicketId, // EventTicketId'si burada kullanılıyor
                        EventId = eventId,  // EventId burada atanmalı
                        IsAvailable = true,
                        UserId = model.UserId // UserId null olabilir, o yüzden nullable bırakıyoruz
                    };

                    // Bilet oluşturma işlemi
                    AddTicket(ticketModel); // Ticket'ı ekle
                }

                return RedirectToAction("Index");


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bilet oluşturma sırasında hata oluştu");
                ModelState.AddModelError("", "Bilet oluşturulurken bir hata meydana geldi.");
                return View(model);
            }
        }

        private void AddTicket(TicketCreateDto ticketModel)
        {
            var ticket = new Ticket
            {
                EventsTicketId = ticketModel.EventsTicketId,
                TicketNumber = GenerateTicketNumber(), // Ticket numarasını burada oluşturun
                IsAvailable = ticketModel.IsAvailable,
                UserId = ticketModel.UserId, // UserId nullable olabilir, null olabilir
                EventId = ticketModel.EventId
            };

            // Eğer UserId null ise, biletin "unassigned" (atama yapılmamış) olarak işaretlenmesi gerekebilir
            if (ticket.UserId == null)
            {
                // Burada null UserId için ek bir işlem yapabilirsiniz
                // Örneğin, biletin "atama yapılmadı" durumu ile kaydedilmesi
                ticket.UserId = null; // Veya başka bir mantık eklenebilir
            }

            // Burada ticket nesnesini veritabanına ekleyin
            _ticketService.TAdd(ticket); // Veya toplu ekleme metodu kullanılabilir
        }


        private string GenerateTicketNumber()
        {
            // Ticket numarası üretme işlemi
            return Guid.NewGuid().ToString("N");
        }






        [HttpGet]
        public IActionResult Edit(int id)
        {
            // Etkinlik biletini veritabanından al
            var eventTicket = _eventsTicketService.TGetByID(id);
            if (eventTicket == null)
            {
                // Bilet bulunamadığında logla ve Index sayfasına yönlendir
                _logger.LogWarning($"Etkinlik bileti bulunamadı: {id}");
                return RedirectToAction("Index");
            }

            // ViewModel oluştur
            var model = new EventTicketEditDto
            {
                EventsTicketId = eventTicket.EventsTicketId,
                TicketCapacity = eventTicket.TicketCapacity,
                Price = eventTicket.Price,
                Name = eventTicket.Name,
                EventId = eventTicket.EventId
            };

            // Etkinlikler
            var events = _eventService.GetAllEvents();
            if (events != null && events.Any())
            {
                // Etkinlik listelerini ViewBag ile gönder
                ViewBag.Events = new SelectList(events, "EventId", "Title");
            }

            // Düzenleme sayfasını model ile birlikte döndür
            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(EventTicketEditDto model)
        {
            // Event listelerini tekrar doldur
            var events = _eventService.GetAllEvents();
            if (events != null && events.Any())
            {
                ViewBag.Events = new SelectList(events, "EventId", "Title");
            }

            // ModelState kontrolü
            if (!ModelState.IsValid)
            {
                // Hata mesajlarını logla
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                foreach (var error in errors)
                {
                    _logger.LogWarning(error);
                }
                return View(model); // Hatalar varsa formu geri gönder
            }

            try
            {
                // Etkinlik bileti verilerini al
                var existingEventTicket = _eventsTicketService.TGetByID(model.EventsTicketId);
                if (existingEventTicket == null)
                {
                    ModelState.AddModelError("", "Güncellenecek etkinlik bileti bulunamadı.");
                    return View(model);
                }

                // Kapasite farkını hesaplayın
                var currentCapacity = existingEventTicket.TicketCapacity;
                var newCapacity = model.TicketCapacity;
                var capacityDifference = newCapacity - currentCapacity;

                // Mevcut EventsTicket'ı güncelle
                existingEventTicket.Price = model.Price;
                existingEventTicket.TicketCapacity = newCapacity;
                existingEventTicket.Name = model.Name;
                _eventsTicketService.TUpdate(existingEventTicket);

                // Eğer kapasite artışı varsa yeni biletler ekleyin
                if (capacityDifference > 0)
                {
                    var ticketsToAdd = new List<Ticket>();

                    for (int i = 0; i < capacityDifference; i++)
                    {
                        
                       

                        var ticket = new Ticket
                        {
                            EventsTicketId = existingEventTicket.EventsTicketId,
                            TicketNumber = GenerateTicketNumber(),
                            IsAvailable = true,
                            EventId = existingEventTicket.EventId // Doğru EventId ataması yapılmalı
                        };

                        ticketsToAdd.Add(ticket); // Listeye ekle
                    }

                    // Toplu ekleme yapmak için
                    if (ticketsToAdd.Any())
                    {
                        _ticketService.TAddRange(ticketsToAdd); // Toplu ekleme fonksiyonu kullan
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bilet güncellemesi sırasında hata oluştu");
                ModelState.AddModelError("", "Bilet güncellenirken bir hata meydana geldi.");
                return View(model);
            }
        }









        // Bilet silme işlemi BURAYA BAK TEKRAR
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var ticket = _eventsTicketService.TGetByID(id);
            if (ticket != null)
            {
                _eventsTicketService.TDelete(ticket);
                return RedirectToAction("Index");
            }
            return NotFound(); // Bilet bulunmazsa hata sayfası göster
        }
    }
}
