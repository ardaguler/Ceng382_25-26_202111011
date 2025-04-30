using Microsoft.EntityFrameworkCore; // Entity Framework Core için gerekli
using YourProjectNamespace.Models; // Daha önce oluşturduğun Class modelini kullanmak için

// Buradaki "YourProjectNamespace" kısmını kendi projenin adıyla değiştirmen gerekebilir.
namespace YourProjectNamespace.Data
{
    public class SchoolDbContext : DbContext
    {
        // Constructor (Yapıcı Metod): DbContext ayarlarını alır.
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
            : base(options)
        {
        }

        // Veritabanındaki 'Classes' tablosuna karşılık gelen DbSet.
        // Entity Framework Core bu özellik üzerinden tablo işlemlerini (CRUD) yapar.
        public DbSet<Class> Classes { get; set; }
    }
}