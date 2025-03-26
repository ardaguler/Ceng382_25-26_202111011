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
        public ClassInformationModel ClassItem { get; set; }

        public List<ClassInformationModel> ClassList => _classList;

        public void OnGet()
        {
        }

        public IActionResult OnPostAdd()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = _classList.FirstOrDefault(x => x.Id == ClassItem.Id);
            if (existing != null)
            {
                existing.ClassName = ClassItem.ClassName;
                existing.StudentCount = ClassItem.StudentCount;
                existing.Description = ClassItem.Description;
            }
            else
            {
                ClassItem.Id = _nextId++;
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
            }
            return RedirectToPage();
        }

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
            }
            return Page();
        }
    }
}
