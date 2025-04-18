// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Week5Lab.Models;      // Your model classes
using Week5Lab.Utilities;   // For the Utils class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;          // For Encoding
using System.Text.Json;     // For JsonSerializerOptions (optional here)

        // Prompt: "How do I create a Razor Page handler to export all data from _allClassList to a JSON file?"
        // Prompt: "Show me how to export only the filtered data (using existing filter parameters) to JSON."
        // Prompt: "How can I modify the JSON export to only include specific columns passed via a 'selectedColumns' query parameter?"
        // Prompt: "Write the C# code for an OnGetExportJson handler that takes an 'exportMode' parameter ('all' or 'filtered') and exports data accordingly."
        // Prompt: "How do I return a JSON string as a downloadable file with a dynamic filename in ASP.NET Core?"
        // Prompt: "Add logic to the export handler to handle the 'currentPage' export mode, exporting only the items visible on the current page."



namespace Week5Lab.Pages
{
    public class IndexModel : PageModel
    {
        // --- Static Data Store (using ClassInformationModel) ---
        private static List<ClassInformationModel> _allClassList = new();
        private static bool _dataInitialized = false;
        private static readonly object _lock = new object();

        // Static constructor
        static IndexModel()
        {
            InitializeData();
        }

        // InitializeData method
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
                         ClassName = $"Class {Convert.ToChar(65 + random.Next(0, 26))}{i}", // Assuming ClassName init handles warning
                         StudentCount = random.Next(10, 51),
                         Description = $"Description for class {i}. Some details here." // Assuming Description init handles warning
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
        public int CurrentPage { get; set; } = 1; // This holds the current page for display

        public int PageSize { get; set; } = 10; // Page size used for display and export
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // --- Properties for the Form and Display ---
        [BindProperty]
        public ClassInformationModel ClassItem { get; set; } = new();

        public List<ClassInformationTable> DisplayedClassList { get; set; } = new();
        public bool IsEditing { get; set; } = false;

        // --- Helper: Refactored Filtering Logic ---
        private IQueryable<ClassInformationModel> GetFilteredQuery(string? searchClassName, int? minStudentCount, int? maxStudentCount)
        {
            InitializeData();
            IQueryable<ClassInformationModel> query = _allClassList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchClassName))
            {
                query = query.Where(c => c.ClassName.Contains(searchClassName, StringComparison.OrdinalIgnoreCase));
            }
            if (minStudentCount.HasValue)
            {
                query = query.Where(c => c.StudentCount >= minStudentCount.Value);
            }
            if (maxStudentCount.HasValue)
            {
                query = query.Where(c => c.StudentCount <= maxStudentCount.Value);
            }
            return query;
        }


        // --- OnGet: Populates the page ---
        public void OnGet()
        {
            var query = GetFilteredQuery(SearchClassName, MinStudentCount, MaxStudentCount);

            TotalCount = query.Count();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            // Use the CurrentPage property bound from the request or default
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages == 0 ? 1 : TotalPages));

            DisplayedClassList = query
                .OrderBy(c => c.Id) // Assuming default order by ID
                .Skip((CurrentPage - 1) * PageSize) // Apply pagination for display
                .Take(PageSize)
                .Select(c => new ClassInformationTable
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    StudentCount = c.StudentCount,
                    Description = c.Description
                })
                .ToList();

            if (!IsEditing)
            {
               ClassItem = new ClassInformationModel();
            }
        }

        // --- OnPost Methods (Add, Delete, Edit remain unchanged) ---
         // ... (OnPostAdd, OnPostDelete, FilterMatches, OnPostEdit code remains exactly the same as before) ...
         public IActionResult OnPostAdd()
         {
             ModelState.Remove("SearchClassName");
             ModelState.Remove("MinStudentCount");
             ModelState.Remove("MaxStudentCount");

             if (!ModelState.IsValid)
             {
                  OnGet();
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
                 var newItem = new ClassInformationModel
                 {
                     Id = nextId,
                     ClassName = ClassItem.ClassName,
                     StudentCount = ClassItem.StudentCount,
                     Description = ClassItem.Description
                 };
                 _allClassList.Add(newItem);
             }

             return RedirectToPage(new { currentPage = CurrentPage, SearchClassName=SearchClassName, MinStudentCount=MinStudentCount, MaxStudentCount=MaxStudentCount });
         }

         public IActionResult OnPostDelete(int id)
         {
             var item = _allClassList.FirstOrDefault(x => x.Id == id);
             if (item != null)
             {
                 _allClassList.Remove(item);
             }

             int totalMatchingAfterDelete = GetFilteredQuery(SearchClassName, MinStudentCount, MaxStudentCount).Count();
             int potentialLastPage = (int)Math.Ceiling(totalMatchingAfterDelete / (double)PageSize);
             int pageToRedirect = Math.Min(CurrentPage, Math.Max(1, potentialLastPage == 0 ? 1 : potentialLastPage));

             return RedirectToPage(new { currentPage = pageToRedirect, SearchClassName=SearchClassName, MinStudentCount=MinStudentCount, MaxStudentCount=MaxStudentCount });
         }

          private bool FilterMatches(ClassInformationModel c)
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
             var item = _allClassList.FirstOrDefault(x => x.Id == id);
             if (item != null)
             {
                 ClassItem = new ClassInformationModel
                 {
                     Id = item.Id,
                     ClassName = item.ClassName,
                     StudentCount = item.StudentCount,
                     Description = item.Description
                 };
                 IsEditing = true;
             }
             else
             {
                 IsEditing = false;
             }
             OnGet();
             return Page();
         }


         // --- *** UPDATED ***: Handler for JSON Export (Supports 'all', 'filtered', 'currentPage') ---
         public IActionResult OnGetExportJson(
             [FromQuery] string exportMode, // "all", "filtered", or "currentPage"
             [FromQuery] string[]? selectedColumns,
             // Filter parameters (always passed, used by 'filtered' and 'currentPage')
             [FromQuery] string? searchClassName,
             [FromQuery] int? minStudentCount,
             [FromQuery] int? maxStudentCount,
             // Pagination parameter (only used by 'currentPage')
             [FromQuery] int? currentPage // Renamed parameter from previous thought process to match JS param
            )
         {
             IEnumerable<ClassInformationModel> dataToExport;
             string effectiveMode = exportMode?.ToLowerInvariant() ?? "all"; // Default to 'all' if mode is missing

             // Determine the data set based on the export mode
             switch (effectiveMode)
             {
                 case "currentpage":
                     // 1. Filter
                     var queryPage = GetFilteredQuery(searchClassName, minStudentCount, maxStudentCount);
                     // 2. Determine page number (use parameter, default to 1)
                     int pageNum = Math.Max(1, currentPage.GetValueOrDefault(1));
                     // 3. Apply ordering and pagination
                     dataToExport = queryPage
                                        .OrderBy(c => c.Id) // Match display order if needed
                                        .Skip((pageNum - 1) * PageSize)
                                        .Take(PageSize)
                                        .ToList();
                     break;

                 case "filtered":
                     // 1. Filter only (no pagination)
                     dataToExport = GetFilteredQuery(searchClassName, minStudentCount, maxStudentCount)
                                        .OrderBy(c => c.Id) // Optional: Order all filtered results
                                        .ToList();
                     break;

                 case "all":
                 default: // Default to exporting all data
                     InitializeData();
                     dataToExport = _allClassList
                                        .OrderBy(c => c.Id) // Optional: Order all results
                                        .ToList(); // Get a copy of the full list
                     break;
             }


             // --- Column Selection Logic (Remains the same) ---
             string jsonString;
             if (selectedColumns != null && selectedColumns.Length > 0)
             {
                 var projectedData = dataToExport.Select(item =>
                 {
                     var dict = new Dictionary<string, object>();
                     foreach (var colName in selectedColumns)
                     {
                         switch (colName.Trim().ToLowerInvariant()) // Use ToLowerInvariant
                         {
                             case "id": dict[colName] = item.Id; break;
                             case "classname": dict[colName] = item.ClassName; break;
                             case "studentcount": dict[colName] = item.StudentCount; break;
                             case "description": dict[colName] = item.Description ?? string.Empty; break;
                         }
                     }
                     return dict;
                 }).ToList();
                 // Serialize projected data (Dictionary)
                 jsonString = Utils.Instance.ConvertToJsonString<Dictionary<string, object>>(projectedData, prettyPrint: true);
             }
             else
             {
                 // Serialize original data (ClassInformationModel)
                 jsonString = Utils.Instance.ConvertToJsonString<ClassInformationModel>(dataToExport, prettyPrint: true);
             }

             // --- Error Check & File Return (Remains the same) ---
             if (jsonString.Contains("error\":\"Failed to serialize data"))
             {
                 Console.Error.WriteLine($"Export failed: Serialization error detected by Utils class for mode '{exportMode}'.");
                 TempData["ExportError"] = "Could not generate the JSON file due to an internal error.";
                 // Redirect back using current page from property if available, else use parameter
                 return RedirectToPage(new { currentPage = this.CurrentPage, SearchClassName = searchClassName, MinStudentCount = minStudentCount, MaxStudentCount = maxStudentCount });
             }

             var fileName = $"class_export_{effectiveMode}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
             return File(Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
         }
    }
}