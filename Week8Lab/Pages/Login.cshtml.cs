using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Veritabanı işlemleri için GEREKLİ
using Week8Lab.Data; // SchoolDbContext için GEREKLİ
using Week8Lab.Models; // User modeli için GEREKLİ
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Session ve Cookie işlemleri için GEREKLİ
using System; // Guid için GEREKLİ
// using Microsoft.AspNetCore.Hosting; // Artık IWebHostEnvironment kullanılmıyor
// using System.IO; // Artık Path, File kullanılmıyor
// using System.Text.Json; // Artık JsonSerializer kullanılmıyor
// using System.Linq; // FirstOrDefaultAsync için EntityFrameworkCore yeterli

namespace Week8Lab.Pages
{
    public class LoginModel : PageModel
    {
        // DbContext'i enjekte et (IWebHostEnvironment yerine)
        private readonly SchoolDbContext _context;

        // Constructor'ı DbContext alacak şekilde güncelle
        public LoginModel(SchoolDbContext context)
        {
            _context = context;
        }

        // Formdan gelen inputlar için özellikler (Aynı kalabilir)
        [BindProperty]
        [Required(ErrorMessage = "Username is required.")]
        public string InputUsername { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string InputPassword { get; set; } = string.Empty;

        // Hata mesajı için özellik (Aynı kalabilir)
        public string? ErrorMessage { get; set; }

        // OnGet metodu genellikle aynı kalabilir
        public void OnGet()
        {
            // İsteğe bağlı: Zaten giriş yapmış kullanıcıyı yönlendirme
        }

        // OnPostAsync metodunu veritabanı kullanacak şekilde güncelle
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Form geçerli değilse sayfayı tekrar göster
            }

            // --- VERİTABANI KULLANARAK GİRİŞ MANTIĞI ---
            try
            {
                // 1. Veritabanında kullanıcı adını ara (case-insensitive)
                var foundUser = await _context.Users
                                        .FirstOrDefaultAsync(u => u.Username.ToLower() == InputUsername.ToLower());

                // 2. Kullanıcıyı doğrula
                if (foundUser != null) // Kullanıcı bulunduysa
                {
                    // --- GÜVENLİK UYARISI: ŞİFRE KONTROLÜ ---
                    // BURASI KESİNLİKLE GÜVENLİ DEĞİL! Sadece bir örnektir.
                    // Gerçek uygulamada InputPassword'u hash'leyip foundUser.PasswordHash ile
                    // güvenli bir şekilde karşılaştırmanız gerekir.
                    // Örnek (güvenli değil):
                    if (foundUser.PasswordHash == InputPassword)
                    // Güvenli Örnek (BCrypt.Net kütüphanesi varsayılarak):
                    // if (BCrypt.Net.BCrypt.Verify(InputPassword, foundUser.PasswordHash))
                    {
                        // --- Başarılı Giriş ---

                        // a. Token ve Session ID oluştur
                        var token = Guid.NewGuid().ToString();
                        var sessionId = Guid.NewGuid().ToString(); // Yeni session ID

                        // c. Session bilgilerini ayarla
                        HttpContext.Session.SetString("username", foundUser.Username);
                        HttpContext.Session.SetString("token", token);
                        HttpContext.Session.SetString("session_id", sessionId);

                        // d. Cookie seçeneklerini tanımla
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddMinutes(30), // 30 dakika geçerlilik
                            HttpOnly = true,
                            Secure = Request.IsHttps, // Sadece HTTPS ise Secure
                            SameSite = SameSiteMode.Strict
                        };

                        // e. Cookie'leri ayarla
                        Response.Cookies.Append("username", foundUser.Username, cookieOptions);
                        Response.Cookies.Append("token", token, cookieOptions);
                        Response.Cookies.Append("session_id", sessionId, cookieOptions);

                        // f. Ana sayfaya yönlendir
                        return RedirectToPage("/Index");
                    }
                }

                // --- Başarısız Giriş ---
                // Kullanıcı bulunamadı veya şifre yanlış
                ErrorMessage = "Invalid username or password.";
                return Page(); // Hata mesajıyla sayfayı tekrar göster

            }
            // Veritabanı veya başka bir hata oluşursa yakala
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred during login.";
                // Hatanın detayını sunucu loglarına yaz (debugging için önemli)
                Console.Error.WriteLine($"Login Error: {ex}");
                return Page();
            }
        }
    }
}
