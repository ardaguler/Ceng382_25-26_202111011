using System.ComponentModel.DataAnnotations;

namespace Week8Lab.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sınıf adı boş bırakılamaz.")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Range(0, int.MaxValue, ErrorMessage = "Öğrenci sayısı negatif olamaz.")]
        public int PersonCount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}