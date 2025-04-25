using System;

// Projenizin ana namespace'i veya Models klasörünün namespace'i
namespace Week8Lab.Models 
{
    /// <summary>
    /// Kullanıcı giriş bilgilerini temsil eden sınıf.
    /// users.json dosyasındaki yapı ile eşleşir.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Kullanıcı adını alır veya ayarlar.
        /// </summary>
        public string Username { get; set; } = string.Empty; // Null referans uyarılarını önlemek için başlangıç değeri

        /// <summary>
        /// Kullanıcının parolasını alır veya ayarlar.
        /// ÖNEMLİ: Gerçek uygulamalarda bu alan düz metin yerine parolanın hash'lenmiş halini tutmalıdır.
        /// </summary>
        public string Password { get; set; } = string.Empty; // Null referans uyarılarını önlemek için başlangıç değeri

        /// <summary>
        /// Kullanıcıya atanan rolü alır veya ayarlar (Örn: "Administrator", "User").
        /// </summary>
        public string Role { get; set; } = string.Empty; // Null referans uyarılarını önlemek için başlangıç değeri

        /// <summary>
        /// Kullanıcı hesabının aktif olup olmadığını ve giriş yapıp yapamayacağını belirten değeri alır veya ayarlar.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Kullanıcı hesabının oluşturulduğu tarih ve saati alır veya ayarlar.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
