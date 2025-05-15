using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HairSalonApp.Data;
using HairSalonApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Runtime.InteropServices;
using System.Globalization;


namespace HairSalonApp.Controllers
{
    [Authorize]
    [Route("Appointments")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //GET: Reserved hours
        [HttpGet("GetReservedHours")]
        public IActionResult GetReservedHours(string hairdresserId, int year, int month, int day, string hour)
        {
            DateTime selectedTime;
            if (!DateTime.TryParseExact(hour, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedTime))
            {
                return BadRequest("Invalid hour format.");
            }

            selectedTime = new DateTime(year, month, day, selectedTime.Hour, selectedTime.Minute, 0);

            //pobranie wizyt dla przypisanego fryzjera
            var reservedAppointments = _context.Appointment
                .Where(a => a.HairdresserId == hairdresserId &&
                            a.AppointmentDate.Year == year &&
                            a.AppointmentDate.Month == month &&
                            a.AppointmentDate.Day == day)
                .Include(a => a.Service)  // Pobieramy usługę, aby wiedzieć, ile trwa
                .ToList();

            //lista zarezerwowanych godzin
            List<DateTime> blockedHours = new List<DateTime>();

            foreach (var appointment in reservedAppointments)
            {
                var startTime = appointment.AppointmentDate;
                var serviceDurationInHours = appointment.Service.Duration / 60;

                //dodanie godzin, które są zarezerwowane na podstawie tej wizyty
                for (int i = 0; i < serviceDurationInHours; i++)
                {
                    blockedHours.Add(startTime.AddHours(i));
                }
            }

            //pobranie dostępnych usług w wybranej godzinie
            var availableServices = _context.Service
                .ToList()
                .Where(service =>
                {
                    var serviceEndTime = selectedTime.AddMinutes(service.Duration);
                    if (serviceEndTime.Hour > 19) return false; // Salon działa do 18:00

                    for (var time = selectedTime; time < serviceEndTime; time = time.AddMinutes(60))
                    {
                        if (blockedHours.Contains(time))
                        {
                            return false;
                        }
                    }
                    return true;
                })
                .Select(service => new { service.Id, service.Name, service.Duration, service.Price })
                .ToList();


            ViewBag.ServiceId = availableServices;

            return Json(new
            {
                availableServices,
                blockedHours,

            });
        }


        // GET: Appointments
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            //sprwadzenie czy zalogowany uzytkownik ma role Hairdresser
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            IQueryable<Appointment> applicationDbContext;

            if (isHairdresser)
            {
                applicationDbContext = _context.Appointment
                    .Where(a => a.HairdresserId == user.Id && (a.ServiceId != null || a.ServiceId == null))
                    .Include(a => a.Hairdresser)
                    .Include(a => a.Service)
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppointmentDate);
            }
            else
            {
                applicationDbContext = _context.Appointment
                    .Where(a => a.UserId == user.Id && (a.ServiceId != null || a.ServiceId == null))
                    .Include(a => a.Hairdresser)
                    .Include(a => a.Service)
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppointmentDate);
            }

            //zwrócenie widoku z wynikami
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Appointments/Details/5
        [HttpGet("Details")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Hairdresser)
                .Include(a => a.Service)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {

            // przypisanie aktualnie zalogowanego uzytkownika
            var userId = _userManager.GetUserId(User);

            // pobranie użytkowników z rolą "Hairdresser" (już będzie typu ApplicationUser)
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");

            List<ApplicationUser> applicationUsers = hairdressers.Cast<ApplicationUser>().ToList();

            ViewBag.Hairdressers = applicationUsers;
            ViewBag.UserId = new SelectList(new[] { userId }, userId);


            return View();
        }

        // POST: Appointments/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,HairdresserId,AppointmentDate,ServiceId")] Appointment appointment)
        {
            var userId = _userManager.GetUserId(User);

            appointment.UserId = userId;

            if(appointment.AppointmentDate == null)
            {
                ModelState.AddModelError("AppointmentDate", "Godzina lub dzień nie zostały wybrane.");
            }
            else
            {


                var service = await _context.Service.FirstOrDefaultAsync(s => s.Id == appointment.ServiceId);
                if (service == null)
                {
                    ModelState.AddModelError("ServiceId", "Wybrana usługa nie istnieje.");
                }

                //obliczenie czasu zakończenia wizyty
                var appointmentEnd = appointment.AppointmentDate.AddMinutes(service.Duration);

                //sprawdzenie czy godzina miesci sie w przedziale 10-18 oraz czy data nie jest podana wstecz
                if (appointment.AppointmentDate.Minute != 0 || appointment.AppointmentDate.Hour < 10 || appointment.AppointmentDate.Hour > 18 || appointment.AppointmentDate < DateTime.Now)
                {
                    ModelState.AddModelError("AppointmentDate", "Godzina wizyty musi być pełną godziną między 10:00 a 18:00 oraz data nie może być wstecz.");
                }

                //sprawdzenie, czy nie ma konfliktu z innymi wizytami
                var conflictingAppointments = await _context.Appointment
                    .Where(a => a.HairdresserId == appointment.HairdresserId &&
                                ((appointment.AppointmentDate >= a.AppointmentDate && appointment.AppointmentDate < a.AppointmentDate.AddMinutes(a.Service.Duration)) ||
                                 (appointmentEnd > a.AppointmentDate && appointmentEnd <= a.AppointmentDate.AddMinutes(a.Service.Duration)) ||
                                 (appointment.AppointmentDate <= a.AppointmentDate && appointmentEnd >= a.AppointmentDate.AddMinutes(a.Service.Duration))))
                    .AnyAsync();

                if (conflictingAppointments)
                {
                    ModelState.AddModelError("AppointmentDate", "Wybrana godzina koliduje z istniejącą rezerwacją.");
                }

            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas zapisywania danych.");
                }
            }
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
            var applicationUsers = hairdressers.OfType<ApplicationUser>().ToList();
            ViewBag.Hairdressers = applicationUsers.Any() ? applicationUsers : new List<ApplicationUser>();

            var services = _context.Service
                .Select(service => new SelectListItem
                {
                    Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                    Value = service.Id.ToString()
                })
                .ToList();

            ViewBag.ServiceId = services;
            ViewBag.UserId = new SelectList(new[] { userId }, userId);

            return View(appointment);
        }

        // GET: Appointments/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
               .Include(a => a.Hairdresser)
               .Include(a => a.Service)
               .Include(a => a.User)
               .FirstOrDefaultAsync(a => a.Id == id);


            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            //sprawdzenie czy uzytkownik jest przypisany do wizyty
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid();
            }

            //pobranie wizyt i fryzjerów
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
            List<ApplicationUser> applicationUsers = hairdressers.Cast<ApplicationUser>().ToList();

            var services = _context.Service
                .Select(service => new SelectListItem
                {
                    Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                    Value = service.Id.ToString()
                })
                .ToList();

            ViewBag.Hairdressers = applicationUsers;
            ViewBag.ServiceId = services;
            ViewBag.UserId = new SelectList(new[] { appointment.UserId }, appointment.UserId);

            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,HairdresserId,AppointmentDate,ServiceId")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            var originalAppointment = await _context.Appointment.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (originalAppointment == null)
            {
                return NotFound();
            }

            appointment.UserId = originalAppointment.UserId;

            if (appointment.UserId != user.Id && (isHairdresser && appointment.HairdresserId != user.Id))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    var service = await _context.Service.FirstOrDefaultAsync(s => s.Id == appointment.ServiceId);
                    if (service == null)
                    {
                        ModelState.AddModelError("ServiceId", "Wybrana usługa nie istnieje.");
                        goto PrepareViewData;
                    }

                    var appointmentEnd = appointment.AppointmentDate.AddMinutes(service.Duration);

                    // sprawdzenie konfliktu z innymi godzinami
                    var conflictingAppointments = await _context.Appointment
                        .Where(a => a.Id != appointment.Id && // wykluczenie aktualnej wizyty
                                   a.HairdresserId == appointment.HairdresserId &&
                                   ((appointment.AppointmentDate >= a.AppointmentDate && appointment.AppointmentDate < a.AppointmentDate.AddMinutes(a.Service.Duration)) ||
                                    (appointmentEnd > a.AppointmentDate && appointmentEnd <= a.AppointmentDate.AddMinutes(a.Service.Duration)) ||
                                    (appointment.AppointmentDate <= a.AppointmentDate && appointmentEnd >= a.AppointmentDate.AddMinutes(a.Service.Duration))))
                        .AnyAsync();

                    if (conflictingAppointments)
                    {
                        ModelState.AddModelError("AppointmentDate", "Wybrana godzina koliduje z istniejącą rezerwacją.");
                        goto PrepareViewData;
                    }

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            PrepareViewData:
                var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");
                var applicationUsers = hairdressers.OfType<ApplicationUser>().ToList();

                var services = _context.Service
                    .Select(service => new SelectListItem
                    {
                        Text = $"{service.Name} - {service.Duration / 60} godz. Cena usługi:",
                        Value = service.Id.ToString()
                    })
                    .ToList();

                ViewBag.Hairdressers = applicationUsers;
                ViewBag.ServiceId = services;
                ViewBag.UserId = new SelectList(new[] { appointment.UserId }, appointment.UserId);

            return View(appointment);
        }

        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointment
                .Include(a => a.Hairdresser)
                .Include(a => a.Service)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            //sprawdzenie, czy użytkownik jest właścicielem wizyty lub przypisanym fryzjerem
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment
        .Include(a => a.Hairdresser)
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var isHairdresser = await _userManager.IsInRoleAsync(user, "Hairdresser");

            //sprawdzenie, czy użytkownik jest właścicielem wizyty lub przypisanym fryzjerem
            if (appointment.UserId != user.Id && (!isHairdresser || appointment.HairdresserId != user.Id))
            {
                return Forbid(); // Blokowanie dostępu, jeśli użytkownik nie ma uprawnień
            }

            //usuwanie wizyty
            _context.Appointment.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointment.Any(e => e.Id == id);
        }
    }
}
