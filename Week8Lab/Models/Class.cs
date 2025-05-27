// "Entity Framework Core için bir model sınıfı oluşturmak istiyorum. 
// Sınıfımın adı 'Class' olacak. İçermesi gereken özellikler şunlar: 
// 'Id' (int, birincil anahtar), 'Name' (string, zorunlu alan), 'PersonCount' (int, zorunlu alan), 
// 'Description' (string) ve 'IsActive' (bool, zorunlu alan). Bu sınıf için C# kodunu DataAnnotations 
// kullanarak yazar mısın?"

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