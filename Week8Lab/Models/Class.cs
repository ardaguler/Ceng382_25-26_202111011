using System.ComponentModel.DataAnnotations; // Bu satırı ekle

// Buradaki "YourProjectNamespace" kısmını kendi projenin adıyla değiştirmen gerekebilir.
namespace YourProjectNamespace.Models
{
    public class Class
    {
        [Key] // Bu özellik birincil anahtar (Primary Key) olacak. [cite: 8]
        public int Id { get; set; }

        [Required] // Bu alanın doldurulması zorunlu. [cite: 8]
        public string Name { get; set; }

        [Required] // Bu alanın doldurulması zorunlu. [cite: 8]
        public int PersonCount { get; set; }

        public string Description { get; set; } // Bu alan zorunlu değil. [cite: 9]

        [Required] // Bu alanın doldurulması zorunlu. [cite: 9]
        public bool IsActive { get; set; }
    }
}