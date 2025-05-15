using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace HairSalonApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        // Powiązanie z użytkownikiem (klientem)
        [Required]
        public string UserId { get; set; }
        [ValidateNever]
        public ApplicationUser? User { get; set; }

        // Powiązanie z fryzjerem
        [Required]
        public string HairdresserId { get; set; }
        [ValidateNever]
        [Display(Name = "Fryzjer")]
        public ApplicationUser? Hairdresser { get; set; }

        // Data i godzina wizyty
        [Required(ErrorMessage = "Upewnij się, że został wybrany dzień oraz godzina wizyty!")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data i godzina wizyty")]
        public DateTime AppointmentDate { get; set; }

        // Powiązanie z usługą
        [Required(ErrorMessage = "Upewnij się, że została wybrana usługa!")]
        public int? ServiceId { get; set; }
        [ValidateNever]
        [Display(Name = "Usługa")]
        public Service? Service { get; set; }
    }
}