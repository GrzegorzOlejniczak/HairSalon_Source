using System.ComponentModel.DataAnnotations;

namespace HairSalonApp.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Display(Name = "Nazwa usługi")]
        [Required(ErrorMessage = "Nazwa jest wymagana.")]
        public string Name { get; set; }

        [Display(Name = "Czas trwania")]
        [Range(1, 10, ErrorMessage = "Czas trwania musi być liczbą od 1 do 10.")]
        public int Duration { get; set; }

        [Display(Name = "Koszt")]
        [Required(ErrorMessage = "Cena jest wymagana.")]
        [Range(0, double.MaxValue, ErrorMessage = "Cena musi być większa lub równa 0.")]
        public decimal Price { get; set; }
    }
}
