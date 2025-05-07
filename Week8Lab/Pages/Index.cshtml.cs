using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Week8Lab.Models;
using Week8Lab.Data;
// using Week8Lab.Utilities; // Utils sınıfı için (Eğer hala kullanılıyorsa)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Week8Lab.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(SchoolDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
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
        public Class ClassItem { get; set; } = new();
        public IList<Class> DisplayedClassList { get; set; } = new List<Class>();
        [BindProperty]
        public bool IsEditing { get; set; } = false;


        // --- OnGetAsync ---
        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");
            
            await LoadDisplayedListAsync();
            if (!IsEditing)
            {
                ClassItem = new Class();
            }
            return Page();
        }

        // --- OnPostAddAsync --- (Hem Ekleme Hem Güncelleme için)
        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            ModelState.Remove("SearchClassName");
            ModelState.Remove("MinStudentCount");
            ModelState.Remove("MaxStudentCount");

            if (IsEditing)
            {
                if (ClassItem.Id == 0)
                {
                    ModelState.AddModelError("ClassItem.Id", "Valid Class ID is required for editing.");
                }
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for ClassItem. IsEditing: {IsEditing}", IsEditing);
                await LoadDisplayedListAsync();
                return Page();
            }

            if (IsEditing)
            {
                // --- GÜNCELLEME MANTIĞI ---
                var classToUpdate = await _context.Classes.FindAsync(ClassItem.Id);

                if (classToUpdate == null)
                {
                    TempData["ErrorMessage"] = $"Class with ID '{ClassItem.Id}' not found for update. It might have been deleted.";
                    _logger.LogWarning("UPDATE FAILED: Attempted to update non-existent Class with ID {ClassId}", ClassItem.Id);
                    IsEditing = false;
                    return RedirectToPage(new { currentPage = CurrentPage, SearchClassName, MinStudentCount, MaxStudentCount });
                }
                
                // IsActive alanı UI'dan kaldırıldığı için, güncelleme sırasında bu alanı
                // TryUpdateModelAsync'in güncelleyeceği özellikler listesinden çıkarıyoruz.
                // Böylece veritabanındaki mevcut IsActive değeri korunur.
                if (await TryUpdateModelAsync<Class>(
                    classToUpdate,
                    "ClassItem",
                    c => c.Name, c => c.PersonCount, c => c.Description /* c => c.IsActive buradan çıkarıldı */))
                {
                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Class '{classToUpdate.Name}' updated successfully.";
                        _logger.LogInformation("Class with ID {ClassId} updated successfully by user.", classToUpdate.Id);
                        IsEditing = false;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex, "CONCURRENCY ERROR while updating Class ID {ClassId}. Handling...", classToUpdate.Id);
                        var exceptionEntry = ex.Entries.Single();
                        var clientValues = (Class)exceptionEntry.Entity;
                        var databaseEntry = exceptionEntry.GetDatabaseValues();

                        if (databaseEntry == null)
                        {
                            TempData["ErrorMessage"] = "The class you attempted to edit was deleted by another user.";
                            ModelState.AddModelError(string.Empty, TempData["ErrorMessage"].ToString());
                            _logger.LogWarning("Concurrency: Class with ID {ClassId} was DELETED by another user during update.", classToUpdate.Id);
                            IsEditing = false;
                        }
                        else
                        {
                            var databaseValues = (Class)databaseEntry.ToObject();
                            TempData["ErrorMessage"] = "The class you attempted to edit was modified by another user. Your changes were not saved. Review the current data and try again if needed.";
                            ModelState.AddModelError(string.Empty, "The record was modified by another user. Current values are shown below.");
                             _logger.LogWarning("Concurrency: Class with ID {ClassId} was MODIFIED by another user. Client Name: '{ClientName}', DB Name: '{DbName}'. Displaying current DB values.",
                                classToUpdate.Id, clientValues.Name, databaseValues.Name);

                            if (databaseValues.Name != clientValues.Name)
                                ModelState.AddModelError("ClassItem.Name", $"Current value: {databaseValues.Name}");
                            if (databaseValues.PersonCount != clientValues.PersonCount)
                                ModelState.AddModelError("ClassItem.PersonCount", $"Current value: {databaseValues.PersonCount}");
                            if (databaseValues.Description != clientValues.Description)
                                ModelState.AddModelError("ClassItem.Description", $"Current value: {databaseValues.Description}");

                            ClassItem.Name = databaseValues.Name;
                            ClassItem.PersonCount = databaseValues.PersonCount;
                            ClassItem.Description = databaseValues.Description;
                            ClassItem.IsActive = databaseValues.IsActive;
                            ClassItem.Id = classToUpdate.Id;
                        }
                        await LoadDisplayedListAsync();
                        return Page();
                    }
                }
                else
                {
                    _logger.LogWarning("Model validation failed for ClassItem during update (TryUpdateModelAsync). ID: {ClassId}", ClassItem.Id);
                    await LoadDisplayedListAsync();
                    return Page();
                }
            }
            else
            {
                // --- YENİ KAYIT EKLEME MANTIĞI ---
                ClassItem.IsActive = true; 
                _context.Classes.Add(ClassItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Class '{ClassItem.Name}' added successfully (and set to active).";
                _logger.LogInformation("New Class '{ClassName}' with ID {ClassId} added successfully by user and IsActive set to TRUE.", ClassItem.Name, ClassItem.Id);
            }

            return RedirectToPage(new { currentPage = CurrentPage, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
        }

        // --- OnPostDeleteAsync --- (SOFT DELETE UYGULANDI)
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            var itemToDeactivate = await _context.Classes.FindAsync(id);
            if (itemToDeactivate != null)
            {
                itemToDeactivate.IsActive = false; // Fiziksel silme yerine IsActive'i false yap
                _context.Entry(itemToDeactivate).State = EntityState.Modified; // Durumu güncellendi olarak işaretle
                // Alternatif olarak: _context.Update(itemToDeactivate); de kullanılabilir.

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Class '{itemToDeactivate.Name}' has been deactivated and removed from the list.";
                    _logger.LogInformation("Class '{ClassName}' with ID {ClassId} was DEACTIVATED (soft delete).", itemToDeactivate.Name, id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                     _logger.LogError(ex, "CONCURRENCY ERROR while deactivating Class ID {ClassId}.", id);
                    // Eş zamanlılık hatası durumunda kullanıcıya bilgi ver
                    TempData["ErrorMessage"] = "Could not deactivate the class. It might have been modified or deleted by another user. Please refresh and try again.";
                    // İsteğe bağlı olarak, hatanın detaylarına göre daha spesifik bir işlem yapılabilir.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deactivating Class ID {ClassId}.", id);
                    TempData["ErrorMessage"] = "An error occurred while deactivating the class.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Class not found for deactivation.";
                _logger.LogWarning("Attempted to deactivate non-existent Class with ID {ClassId}", id);
            }

            // Sayfa numarasını yeniden hesaplama kısmı aynı kalabilir,
            // çünkü "silinen" (deaktive edilen) öğe artık listede görünmeyecek (eğer listeleme sorgusu IsActive=true'ya göre filtreleniyorsa).
            var query = GetFilteredQuery(); // Bu metodun IsActive=true filtresini içerdiğini varsayıyoruz.
            int totalMatchingAfterDeactivate = await query.CountAsync();
            int potentialLastPage = (int)Math.Ceiling(totalMatchingAfterDeactivate / (double)PageSize);
            CurrentPage = Math.Min(CurrentPage, Math.Max(1, potentialLastPage == 0 ? 1 : potentialLastPage));

            return RedirectToPage(new { currentPage = CurrentPage, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
        }

        // --- OnPostEditAsync ---
        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");

            ClassItem = await _context.Classes.FindAsync(id);
            if (ClassItem == null)
            {
                TempData["ErrorMessage"] = "Class not found for editing.";
                _logger.LogWarning("Attempted to edit non-existent Class with ID {ClassId}", id);
                IsEditing = false;
                return RedirectToPage(new { currentPage = CurrentPage, SearchClassName = SearchClassName, MinStudentCount = MinStudentCount, MaxStudentCount = MaxStudentCount });
            }
            // Soft delete uyguladığımız için, kullanıcı IsActive=false olan bir kaydı düzenlemeye çalışabilir.
            // Eğer IsActive=false olanların düzenlenmesini istemiyorsanız burada bir kontrol ekleyebilirsiniz.
            // Örneğin: if (!ClassItem.IsActive) { TempData["WarningMessage"] = "This class is inactive."; }
            IsEditing = true;
            _logger.LogInformation("User started editing Class with ID {ClassId}", id);
            await LoadDisplayedListAsync();
            return Page();
        }

        // --- OnGetExportJson ---
        public async Task<IActionResult> OnGetExportJson(
             [FromQuery] string exportMode,
             [FromQuery] string[]? selectedColumns,
             [FromQuery] string? searchClassName,
             [FromQuery] int? minStudentCount,
             [FromQuery] int? maxStudentCount,
             [FromQuery] int? currentPage)
        {
            if (!IsUserAuthenticated()) return RedirectToPage("/Login");
            
            IQueryable<Class> query;
            bool useGlobalFilters = string.IsNullOrWhiteSpace(searchClassName) && !minStudentCount.HasValue && !maxStudentCount.HasValue;

            if (useGlobalFilters && (exportMode?.ToLowerInvariant() == "filtered" || exportMode?.ToLowerInvariant() == "currentpage")) {
                 // Eğer export "filtered" veya "currentPage" ise ve query parametreleri boşsa,
                 // PageModel'daki mevcut filtreleri kullan.
                query = GetFilteredQuery();
            } else if (!useGlobalFilters) {
                // Query parametreleri doluysa onları kullan.
                query = _context.Classes.AsQueryable(); // Önce tümünü al
                 if (!string.IsNullOrWhiteSpace(searchClassName))
                {
                    query = query.Where(c => c.Name.ToLower().Contains(searchClassName.ToLower()));
                }
                if (minStudentCount.HasValue)
                {
                    query = query.Where(c => c.PersonCount >= minStudentCount.Value);
                }
                if (maxStudentCount.HasValue)
                {
                    query = query.Where(c => c.PersonCount <= maxStudentCount.Value);
                }
                // Soft delete uygulandığı için, export ederken de sadece aktif olanları mı,
                // yoksa tümünü mü (IsActive durumuna bakılmaksızın) export edeceğinize karar vermelisiniz.
                // Mevcut durumda yukarıdaki filtreler IsActive'i içermiyor.
                // Eğer sadece aktifler export edilecekse: query = query.Where(c => c.IsActive); eklenmeli.
            }
            else // "all" export modu ve query parametreleri boşsa, tümünü (aktif/pasif) alır.
            {
                 query = _context.Classes.AsQueryable();
                 // "all" modunda tüm veriler (IsActive durumuna bakılmaksızın) çekilir.
                 // Eğer "all" modunda bile sadece aktifler istenseydi, buraya da IsActive filtresi eklenirdi.
            }


            IEnumerable<Class> dataToExport;
            string effectiveMode = exportMode?.ToLowerInvariant() ?? "all";

            _logger.LogInformation("Exporting classes. Mode: {ExportMode}, Columns: {SelectedColumnsCount}", 
                effectiveMode, selectedColumns?.Length ?? 0);

            switch (effectiveMode)
            {
                case "currentpage":
                    int pageNum = Math.Max(1, currentPage.GetValueOrDefault(this.CurrentPage));
                    int pageSizeVal = this.PageSize;
                    dataToExport = await query.OrderBy(c => c.Id).Skip((pageNum - 1) * pageSizeVal).Take(pageSizeVal).ToListAsync();
                    break;
                case "filtered":
                    dataToExport = await query.OrderBy(c => c.Id).ToListAsync();
                    break;
                case "all":
                default: // "all" modunda tümünü (aktif/pasif) alır, filtreleme yukarıda yapıldı.
                    dataToExport = await query.OrderBy(c => c.Id).ToListAsync();
                    break;
            }

            string jsonString = SerializeToJson(dataToExport, selectedColumns);
            if (jsonString.Contains("error\":\"Failed to serialize data"))
            {
                TempData["ExportError"] = "Could not generate the JSON file due to an internal error.";
                _logger.LogError("JSON Export failed: Serialization error indicated in JSON string.");
                return RedirectToPage(new { currentPage = this.CurrentPage, SearchClassName = this.SearchClassName, MinStudentCount = this.MinStudentCount, MaxStudentCount = this.MaxStudentCount });
            }

            var fileName = $"class_export_{effectiveMode}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            return File(Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
        }


        // --- Yardımcı Metodlar ---
        private IQueryable<Class> GetFilteredQuery()
        {
            var query = _context.Classes.AsQueryable();

            // <<< --- ÖNEMLİ DEĞİŞİKLİK (SOFT DELETE İÇİN) --- >>>
            // Sadece IsActive = true olan sınıfları listele
            query = query.Where(c => c.IsActive);


            if (!string.IsNullOrWhiteSpace(SearchClassName))
            {
                string lowerSearchClassName = SearchClassName.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(lowerSearchClassName));
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

        private async Task LoadDisplayedListAsync()
        {
            try
            {
                var query = GetFilteredQuery(); // Bu metod artık IsActive=true filtresini içeriyor.
                TotalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
                CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

                DisplayedClassList = await query
                   .OrderBy(c => c.Id)
                   .Skip((CurrentPage - 1) * PageSize)
                   .Take(PageSize)
                   .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading displayed list. Search: '{Search}', MinStudents: {Min}, MaxStudents: {Max}",
                   SearchClassName, MinStudentCount, MaxStudentCount);
                TempData["ErrorMessage"] = "An error occurred while loading or filtering the class list.";
                DisplayedClassList = new List<Class>();
                TotalCount = 0;
                TotalPages = 0;
            }
        }

        private string SerializeToJson(IEnumerable<Class> data, string[]? selectedColumns)
        {
            try
            {
                object dataToSerialize;
                if (selectedColumns != null && selectedColumns.Any() && selectedColumns.All(s => !string.IsNullOrWhiteSpace(s)))
                {
                    dataToSerialize = data.Select(item =>
                    {
                        var dict = new Dictionary<string, object?>();
                        foreach (var colName in selectedColumns)
                        {
                            if (string.Equals(colName, nameof(Class.Id), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Id;
                            else if (string.Equals(colName, nameof(Class.Name), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Name;
                            else if (string.Equals(colName, nameof(Class.PersonCount), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.PersonCount;
                            else if (string.Equals(colName, nameof(Class.Description), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.Description;
                            else if (string.Equals(colName, nameof(Class.IsActive), StringComparison.OrdinalIgnoreCase)) dict[colName] = item.IsActive;
                            else _logger.LogWarning("Unknown column name '{ColumnName}' provided for JSON export.", colName);
                        }
                        return dict;
                    }).ToList();
                }
                else
                {
                    dataToSerialize = data;
                }
                return JsonSerializer.Serialize(dataToSerialize, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON Serialization failed.");
                return "{\"error\":\"Failed to serialize data due to an internal error.\"}";
            }
        }

        private bool IsUserAuthenticated()
        {
            string? sessionUsername = HttpContext.Session.GetString("username");
            string? sessionToken = HttpContext.Session.GetString("token");
            string? cookieUsername = Request.Cookies["username"];
            string? cookieToken = Request.Cookies["token"];

            bool isAuthenticated = !string.IsNullOrEmpty(sessionUsername) &&
                                 !string.IsNullOrEmpty(sessionToken) &&
                                 !string.IsNullOrEmpty(cookieUsername) &&
                                 !string.IsNullOrEmpty(cookieToken) &&
                                 sessionUsername == cookieUsername &&
                                 sessionToken == cookieToken;
            return isAuthenticated;
        }
    }
}