using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Week8Lab.Models; // Class ve User modellerinin namespace'i

namespace Week8Lab.Data // DbContext'in namespace'i
{
    public partial class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
            : base(options)
        {
        }

        // Mevcut DbSet (Classes tablosu için)
        public virtual DbSet<Class> Classes { get; set; } = null!;

        // --- YENİ EKLENEN SATIR ---
        // Veritabanındaki 'Users' tablosunu temsil eden DbSet özelliği.
        public virtual DbSet<User> Users { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Scaffold otomatik olarak tablo yapılandırmalarını buraya ekleyebilir.
            // Users tablosu için ekleme yapabiliriz:
            modelBuilder.Entity<User>(entity =>
            {
                // Username kolonunun veritabanı seviyesinde benzersiz (unique) olmasını sağla
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Classes tablosu için scaffold tarafından oluşturulan yapılandırmalar varsa
            // onlar da burada yer alabilir veya aşağıdaki gibi manuel eklenebilir:
            // modelBuilder.Entity<Class>(entity =>
            // {
            //     entity.Property(e => e.Name).HasMaxLength(100);
            // });


            // Mevcut OnModelCreatingPartial çağrısını koru
            OnModelCreatingPartial(modelBuilder);
        }

        // Bu metod, gerekirse başka bir dosyada ek yapılandırma yapılmasına olanak tanır.
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
