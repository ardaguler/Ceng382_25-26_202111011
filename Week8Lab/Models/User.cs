using System.ComponentModel.DataAnnotations;

namespace Week8Lab.Models // Projenin namespace'i
{
    public class User
    {
        [Key] // Birincil anahtar
        public int Id { get; set; }

        [Required] // Zorunlu alan
        [StringLength(100)] // Maksimum uzunluk
        public string Username { get; set; } = null!; // Null atanabilirliği yönetmek için null!

        [Required] // Zorunlu alan
        public string PasswordHash { get; set; } = null!; // Şifrenin hash'lenmiş hali saklanacak

        // İsteğe bağlı olarak başka alanlar eklenebilir (Email, Ad, Soyad vb.)
        // public string? Email { get; set; }
        // public string? FullName { get; set; }
    }
}
