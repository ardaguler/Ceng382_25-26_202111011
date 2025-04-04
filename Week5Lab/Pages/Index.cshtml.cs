using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Week5Lab.Models;

namespace Week5Lab.Pages
{
    public class IndexModel : PageModel
    {
        private static List<ClassInformationModel> _classList = new();
        private static int _nextId = 1;

        [BindProperty]
        public ClassInformationModel ClassItem { get; set; } = new();

        public List<ClassInformationModel> ClassList => _classList;

        public void OnGet()
        {
            ClassItem = new ClassInformationModel();
            IsEditing = false;
        }

        public IActionResult OnPostAdd()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Eğer düzenleme yapılacaksa (ID > 0)
            if (ClassItem.Id > 0)
            {
                var existing = _classList.FirstOrDefault(x => x.Id == ClassItem.Id);
                if (existing != null)
                {
                    existing.ClassName = ClassItem.ClassName;
                    existing.StudentCount = ClassItem.StudentCount;
                    existing.Description = ClassItem.Description;
                }
            }
            else
            {
                // ✅ En küçük boş ID'yi bul
                var usedIds = _classList.Select(x => x.Id).OrderBy(id => id).ToList();
                int nextId = 1;
                foreach (var id in usedIds)
                {
                    if (id != nextId) break;
                    nextId++;
                }

                ClassItem.Id = nextId;
                _classList.Add(ClassItem);
            }

            return RedirectToPage();
        }



        public IActionResult OnPostDelete(int id)
        {
            var item = _classList.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _classList.Remove(item);

                // ✅ SİLİNDİKTEN SONRA TÜM ID'LERİ BAŞTAN NUMARALANDIR
                int counter = 1;
                foreach (var c in _classList.OrderBy(x => x.Id))
                {
                    c.Id = counter++;
                }
            }

            return RedirectToPage();
        }

        public bool IsEditing { get; set; } = false;

        public IActionResult OnPostEdit(int id)
        {
            var item = _classList.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                ClassItem = new ClassInformationModel
                {
                    Id = item.Id,
                    ClassName = item.ClassName,
                    StudentCount = item.StudentCount,
                    Description = item.Description
                };

                IsEditing = true; // ✅ Edit moduna gir
            }

            return Page();
        }

    }
}
