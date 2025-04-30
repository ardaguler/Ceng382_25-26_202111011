using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // DbContext, ToListAsync, CountAsync, FindAsync, EntityState vb. için
using YourProjectNamespace.Models; // 'Class' modelinin namespace'i - KENDİNE GÖRE DÜZENLE
using YourProjectNamespace.Data;    // SchoolDbContext'in namespace'i - KENDİNE GÖRE DÜZENLE
using Week8Lab.Utilities;   // Utils sınıfı için (Eğer hala kullanılıyorsa)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks; // async/await için

// Buradaki "YourProjectNamespace" kısımlarını kendi projenle değiştir.
namespace Week8Lab.Pages // Namespace'in bu şekilde kalabilir
{
    public class IndexModel : PageModel
    {
        private readonly SchoolDbContext _context; // Veritabanı bağlamı

        public IndexModel(SchoolDbContext context) // DbContext enjeksiyonu
        {
            _context = context;
        }

        // Filtreleme Özellikleri
        [BindProperty(SupportsGet = true)]
        public string? SearchClassName { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? MinStudentCount { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? MaxStudentCount { get; set; }

        // Sayfalama Özellikleri
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Form ve Gösterim Özellikleri
        [BindProperty]
        public Class ClassItem { get; set; } = new(); // DB Modeli
        public IList<Class> DisplayedClassList { get; set; } = new List<Class>(); // DB Modeli Listesi
        [BindProperty] // IsEditing'i postback'ler arasında korumak için BindProperty eklenebilir
        public bool IsEditing { get; set; } = false;


        // --- OnGetAsync: Sayfa yüklendiğinde veriyi DB'den okur ---
        public async Task<IActionResult> OnGetAsync()
        {
            // Oturum Kontrolü (Aynı kalabilir)
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            // Liste verisini yükle (Filtreleme/Sayfalama dahil)
            await LoadDisplayedListAsync();

            // Edit modunda değilsek formu temizle
            if (!IsEditing)
            {
                ClassItem = new Class();
            }
            // Eğer IsEditing true ise, ClassItem OnPostEdit tarafından doldurulmuş olmalı.

            return Page();
        }

        // --- OnPostAdd: Yeni kayıt ekler veya mevcut kaydı günceller ---
        public async Task<IActionResult> OnPostAddAsync() // async Task eklendi
        {
            // Oturum Kontrolü
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            // Filtreleme alanlarını ModelState'den kaldır (formda olmadıkları için)
            ModelState.Remove("SearchClassName");
            ModelState.Remove("MinStudentCount");
            ModelState.Remove("MaxStudentCount");

            // IsEditing durumunu ModelState'den kaldır (eğer BindProperty kullandıysak)
            ModelState.Remove(nameof(IsEditing));


            if (!ModelState.IsValid)
            {
                // Hata varsa, listeyi tekrar yükleyip sayfayı göster
                await LoadDisplayedListAsync();
                return Page();
            }

            if (IsEditing) // Güncelleme işlemi
            {
                // ClassItem zaten formdan gelen güncel verileri içeriyor olmalı (Id dahil)
                _context.Attach(ClassItem).State = EntityState.Modified; // Durumu 'Değiştirildi' yap

                try
                {
                    await _context.SaveChangesAsync(); // Değişiklikleri DB'ye kaydet
                }
                catch (DbUpdateConcurrencyException) // Eş zamanlı güncelleme hatası
                {
                    // Kayıt bulunamadıysa (başka biri silmişse)
                    if (!await _context.Classes.AnyAsync(e => e.Id == ClassItem.Id))
                    {
                        return NotFound();
                    }
                    else // Başka bir concurrency hatasıysa tekrar fırlat
                    {
                        throw;
                    }
                }
                IsEditing = false; // Edit modundan çık
                TempData["SuccessMessage"] = $"Class '{ClassItem.Name}' updated successfully."; // Başarı mesajı
            }
            else // Ekleme işlemi
            {
                // Yeni ClassItem nesnesi formdan geldi.
                _context.Classes.Add(ClassItem); // Yeni kaydı ekle
                await _context.SaveChangesAsync(); // Değişiklikleri DB'ye kaydet
                TempData["SuccessMessage"] = $"Class '{ClassItem.Name}' added successfully."; // Başarı mesajı
            }

            // İşlem sonrası aynı filtre ve sayfa ile sayfaya yönlendir
            return RedirectToPage(new { currentPage = CurrentPage, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
        }

        // --- OnPostDelete: Kayıt siler ---
        public async Task<IActionResult> OnPostDeleteAsync(int id) // async Task eklendi
        {
            // Oturum Kontrolü
             if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            var itemToDelete = await _context.Classes.FindAsync(id); // ID ile kaydı bul

            if (itemToDelete != null)
            {
                _context.Classes.Remove(itemToDelete); // Silme için işaretle
                await _context.SaveChangesAsync();     // Değişiklikleri DB'ye kaydet
                 TempData["SuccessMessage"] = $"Class '{itemToDelete.Name}' deleted successfully."; // Başarı mesajı
            }
            else
            {
                TempData["ErrorMessage"] = "Class not found for deletion."; // Hata mesajı
            }

            // Silme sonrası doğru sayfaya yönlendirmek için sayfa numarasını yeniden hesapla
            var query = GetFilteredQuery(); // Filtrelenmiş sorguyu al
            int totalMatchingAfterDelete = await query.CountAsync();
            int potentialLastPage = (int)Math.Ceiling(totalMatchingAfterDelete / (double)PageSize);
            int pageToRedirect = Math.Min(CurrentPage, Math.Max(1, potentialLastPage == 0 ? 1 : potentialLastPage));


            return RedirectToPage(new { currentPage = pageToRedirect, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
        }

        // --- OnPostEdit: Düzenlenecek kaydı bulur ve formu doldurur ---
        public async Task<IActionResult> OnPostEditAsync(int id) // async Task eklendi
        {
            // Oturum Kontrolü
             if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            ClassItem = await _context.Classes.FindAsync(id); // Düzenlenecek kaydı DB'den bul

            if (ClassItem == null)
            {
                TempData["ErrorMessage"] = "Class not found for editing."; // Hata mesajı
                return RedirectToPage(new { currentPage = CurrentPage, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
                // Veya return NotFound();
            }

            IsEditing = true; // Edit moduna geç

            // Listeyi tekrar yükleyip sayfayı göster (form dolu olacak)
            await LoadDisplayedListAsync();
            return Page();
        }

        // --- OnGetExportJson: Veriyi JSON olarak dışa aktarır ---
        // (Bu metod bir önceki adımda zaten DB'den okuyacak şekilde güncellenmişti, aynı kalabilir)
        public async Task<IActionResult> OnGetExportJson( /* Parametreler */
             [FromQuery] string exportMode,
             [FromQuery] string[]? selectedColumns,
             [FromQuery] string? searchClassName, // Parametre isimleri model ile eşleşmeli
             [FromQuery] int? minStudentCount,
             [FromQuery] int? maxStudentCount,
             [FromQuery] int? currentPage
           )
        {
            // Oturum Kontrolü
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");


            // Veritabanından Export için Veri Çekme (Filtrelenmiş)
            IQueryable<Class> query = GetFilteredQuery(); // Filtreleri uygula

            IEnumerable<Class> dataToExport;
            string effectiveMode = exportMode?.ToLowerInvariant() ?? "all";

            switch (effectiveMode)
            {
                 case "currentpage":
                    int pageNum = Math.Max(1, currentPage.GetValueOrDefault(1));
                    // CurrentPage parametresini kullan, this.CurrentPage değil
                    dataToExport = await query
                                       .OrderBy(c => c.Id)
                                       .Skip((pageNum - 1) * PageSize)
                                       .Take(PageSize)
                                       .ToListAsync();
                    break;
                case "filtered":
                    dataToExport = await query
                                       .OrderBy(c => c.Id)
                                       .ToListAsync();
                    break;
                case "all":
                default:
                    dataToExport = await _context.Classes // Filtresiz tümü
                                       .OrderBy(c => c.Id)
                                       .ToListAsync();
                    break;
            }

             // JSON Oluşturma ve Döndürme (Önceki cevaptaki gibi)
             // ... (JSON serileştirme ve File result döndürme kodu)...
            string jsonString = SerializeToJson(dataToExport, selectedColumns); // Yardımcı metoda taşıyalım

            if (jsonString.Contains("error\":\"Failed to serialize data")) // Hata kontrolü
            {
                 TempData["ExportError"] = "Could not generate the JSON file due to an internal error.";
                  return RedirectToPage(new { currentPage = this.CurrentPage, SearchClassName = this.SearchClassName, MinStudentCount = this.MinStudentCount, MaxStudentCount = this.MaxStudentCount });
            }

             var fileName = $"class_export_{effectiveMode}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
             return File(Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
        }


        // --- Yardımcı Metodlar ---

        // Filtrelenmiş IQueryable<Class> döndüren yardımcı metod
        private IQueryable<Class> GetFilteredQuery()
        {
            var query = _context.Classes.AsQueryable(); // DB sorgusunu başlat

            if (!string.IsNullOrWhiteSpace(SearchClassName))
            {
                query = query.Where(c => c.Name.Contains(SearchClassName, StringComparison.OrdinalIgnoreCase));
            }
            if (MinStudentCount.HasValue)
            {
                query = query.Where(c => c.PersonCount >= MinStudentCount.Value);
            }
            if (MaxStudentCount.HasValue)
            {
                query = query.Where(c => c.PersonCount <= MaxStudentCount.Value);
            }
            return query;
        }

        // Sayfada gösterilecek listeyi yükleyen yardımcı metod
        private async Task LoadDisplayedListAsync()
        {
            var query = GetFilteredQuery(); // Filtrelenmiş sorguyu al

            TotalCount = await query.CountAsync(); // Toplam sayıyı hesapla
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            // CurrentPage değerinin geçerli aralıkta olduğundan emin ol
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            // Veriyi sırala, sayfala ve çek
            DisplayedClassList = await query
                .OrderBy(c => c.Id) // Veya Name'e göre sırala
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        // JSON Serileştirme için yardımcı metod (Opsiyonel)
        private string SerializeToJson(IEnumerable<Class> data, string[]? selectedColumns)
        {
             // Utils sınıfını veya System.Text.Json'ı doğrudan kullanabilirsin
             // Önceki cevaptaki seçili kolon mantığı buraya taşınabilir.
             try
             {
                  object dataToSerialize;
                  if (selectedColumns != null && selectedColumns.Length > 0)
                  {
                        dataToSerialize = data.Select(item =>
                        {
                             var dict = new Dictionary<string, object?>();
                             foreach (var colName in selectedColumns)
                             {
                                 // Class özelliklerine göre doldur...
                                if (string.Equals(colName, nameof(Class.Id), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Id;
                                else if (string.Equals(colName, nameof(Class.Name), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Name;
                                else if (string.Equals(colName, nameof(Class.PersonCount), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.PersonCount;
                                else if (string.Equals(colName, nameof(Class.Description), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Description;
                                else if (string.Equals(colName, nameof(Class.IsActive), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.IsActive;
                             }
                             return dict;
                        }).ToList();
                         return JsonSerializer.Serialize(dataToSerialize, new JsonSerializerOptions { WriteIndented = true });
                  }
                  else
                  {
                       dataToSerialize = data;
                       return JsonSerializer.Serialize(dataToSerialize, new JsonSerializerOptions { WriteIndented = true });
                  }
             }
             catch (Exception ex)
             {
                  Console.Error.WriteLine($"JSON Serialization failed: {ex.Message}");
                  return "{\"error\":\"Failed to serialize data\"}"; // Hata durumu için
             }
        }


        // Oturum/Cookie kontrolü için yardımcı metod
        private bool IsUserAuthenticated()
        {
            string? sessionUsername = HttpContext.Session.GetString("username");
            string? sessionToken = HttpContext.Session.GetString("token");
            string? cookieUsername = Request.Cookies["username"];
            string? cookieToken = Request.Cookies["token"];

            return !string.IsNullOrEmpty(sessionUsername) && !string.IsNullOrEmpty(sessionToken) &&
                   !string.IsNullOrEmpty(cookieUsername) && !string.IsNullOrEmpty(cookieToken) &&
                   sessionUsername == cookieUsername && sessionToken == cookieToken;
        }
    }
}