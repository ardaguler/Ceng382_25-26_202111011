// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Week5Lab.Models; // Make sure this is included
using System;
using System.Collections.Generic;
using System.Linq;

namespace Week5Lab.Pages
{
    public class IndexModel : PageModel
    {
        // --- Static Data Store (using ClassInformationModel) ---
        private static List<ClassInformationModel> _allClassList = new();
        private static bool _dataInitialized = false;
        private static readonly object _lock = new object();

        static IndexModel()
        {
            InitializeData();
        }

        private static void InitializeData()
        {
             lock (_lock)
             {
                 if (_dataInitialized) return;

                 _allClassList = new List<ClassInformationModel>();
                 var random = new Random();
                 for (int i = 1; i <= 115; i++)
                 {
                     _allClassList.Add(new ClassInformationModel
                     {
                         Id = i,
                         ClassName = $"Class {Convert.ToChar(65 + random.Next(0, 26))}{i}",
                         StudentCount = random.Next(10, 51),
                         Description = $"Description for class {i}. Some details here."
                         // Add other properties if ClassInformationModel has them
                     });
                 }
                 _dataInitialized = true;
             }
        }

        // --- Properties for Filtering ---
        [BindProperty(SupportsGet = true)]
        public string? SearchClassName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MinStudentCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MaxStudentCount { get; set; }

        // --- Properties for Pagination ---
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // --- Properties for the Form and Display ---
        [BindProperty]
        public ClassInformationModel ClassItem { get; set; } = new(); // Form uses the original model

        // ***MODIFIED***: Use ClassInformationTable for the display list
        public List<ClassInformationTable> DisplayedClassList { get; set; } = new();

        public bool IsEditing { get; set; } = false;

        // --- OnGet: Filtering and Pagination Logic ---
        public void OnGet()
        {
            InitializeData();

            IQueryable<ClassInformationModel> query = _allClassList.AsQueryable();

            // Apply Filtering
            if (!string.IsNullOrWhiteSpace(SearchClassName))
            {
                query = query.Where(c => c.ClassName.Contains(SearchClassName, StringComparison.OrdinalIgnoreCase));
            }
            if (MinStudentCount.HasValue)
            {
                query = query.Where(c => c.StudentCount >= MinStudentCount.Value);
            }
            if (MaxStudentCount.HasValue)
            {
                query = query.Where(c => c.StudentCount <= MaxStudentCount.Value);
            }

            TotalCount = query.Count();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));


            // Apply Pagination and ***MODIFIED*** Project to ClassInformationTable
            DisplayedClassList = query
                .OrderBy(c => c.Id)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new ClassInformationTable // Project to the new model
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    StudentCount = c.StudentCount,
                    Description = c.Description
                    // Map only the properties needed for the table/actions
                })
                .ToList(); // Execute the query

            if (!IsEditing)
            {
               ClassItem = new ClassInformationModel();
            }
             // OnGet logic continues as before...
        }

        // --- OnPost Methods (Add, Delete, Edit) ---
        // These still operate on the _allClassList which contains ClassInformationModel objects
        // The mapping only happens when preparing data for display in OnGet.

         public IActionResult OnPostAdd()
         {
             ModelState.Remove("SearchClassName");
             ModelState.Remove("MinStudentCount");
             ModelState.Remove("MaxStudentCount");

             if (!ModelState.IsValid)
             {
                  OnGet(); // Repopulate DisplayedClassList (which uses ClassInformationTable)
                  return Page();
             }

             var existing = (ClassItem.Id > 0) ? _allClassList.FirstOrDefault(x => x.Id == ClassItem.Id) : null;

             if (existing != null) // Update
             {
                 existing.ClassName = ClassItem.ClassName;
                 existing.StudentCount = ClassItem.StudentCount;
                 existing.Description = ClassItem.Description;
                 IsEditing = false;
             }
             else // Add
             {
                 int nextId = (_allClassList.Any() ? _allClassList.Max(x => x.Id) : 0) + 1;
                 // Create a new ClassInformationModel for the backend list
                 var newItem = new ClassInformationModel
                 {
                     Id = nextId,
                     ClassName = ClassItem.ClassName,
                     StudentCount = ClassItem.StudentCount,
                     Description = ClassItem.Description
                 };
                 _allClassList.Add(newItem);
             }

             // Redirect to GET, preserving filters/page
             return RedirectToPage(new { currentPage = CurrentPage, SearchClassName=SearchClassName, MinStudentCount=MinStudentCount, MaxStudentCount=MaxStudentCount });
         }

         public IActionResult OnPostDelete(int id)
         {
             var item = _allClassList.FirstOrDefault(x => x.Id == id);
             if (item != null)
             {
                 _allClassList.Remove(item);
             }

             int potentialLastPage = (int)Math.Ceiling((_allClassList.Count(c => FilterMatches(c))) / (double)PageSize);
             int pageToRedirect = Math.Min(CurrentPage, Math.Max(1, potentialLastPage));

             return RedirectToPage(new { currentPage = pageToRedirect, SearchClassName=SearchClassName, MinStudentCount=MinStudentCount, MaxStudentCount=MaxStudentCount });
         }

          // Helper function for filter check used in Delete redirection logic
          private bool FilterMatches(ClassInformationModel c) // Still operates on ClassInformationModel
          {
              bool match = true;
              if (!string.IsNullOrWhiteSpace(SearchClassName))
              {
                  match &= c.ClassName.Contains(SearchClassName, StringComparison.OrdinalIgnoreCase);
              }
              if (MinStudentCount.HasValue)
              {
                  match &= c.StudentCount >= MinStudentCount.Value;
              }
              if (MaxStudentCount.HasValue)
              {
                  match &= c.StudentCount <= MaxStudentCount.Value;
              }
              return match;
          }


         public IActionResult OnPostEdit(int id)
         {
             // Find the item in the main list (_allClassList)
             var item = _allClassList.FirstOrDefault(x => x.Id == id);
             if (item != null)
             {
                  // Populate ClassItem (which is ClassInformationModel) for the form
                 ClassItem = new ClassInformationModel
                 {
                     Id = item.Id,
                     ClassName = item.ClassName,
                     StudentCount = item.StudentCount,
                     Description = item.Description
                     // Map other properties if they exist and are needed for editing
                 };
                 IsEditing = true;
             }
             else
             {
                 IsEditing = false;
                 // Handle item not found? Maybe TempData message?
             }

             OnGet(); // Repopulates DisplayedClassList using ClassInformationTable

             return Page();
         }
    }
}