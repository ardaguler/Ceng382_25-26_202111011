using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Week8Lab.Models;      // Your model classes
using Week8Lab.Utilities;   // For the Utils class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;          // For Encoding
using System.Text.Json;     // For JsonSerializerOptions (optional here)
using Microsoft.AspNetCore.Http; // Required for HttpContext, Session, Cookies

/*
AI Prompts for specific export functionalities in this handler:

// Prompt: "How do I create a Razor Page handler to export all data from _allClassList to a JSON file?"
// Prompt: "Show me how to export only the filtered data (using existing filter parameters) to JSON."
// Prompt: "How can I modify the JSON export to only include specific columns passed via a 'selectedColumns' query parameter?"
// Prompt: "Write the C# code for an OnGetExportJson handler that takes an 'exportMode' parameter ('all' or 'filtered') and exports data accordingly."
// Prompt: "How do I return a JSON string as a downloadable file with a dynamic filename in ASP.NET Core?"
// Prompt: "Add logic to the export handler to handle the 'currentPage' export mode, exporting only the items visible on the current page."

*/

namespace Week8Lab.Pages
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
        // *** UPDATED: Changed return type from void to IActionResult to allow redirection ***
        public IActionResult OnGet()
        {
            // --- ACCESS CONTROL CHECK ---
            // Retrieve values from Session
            string? sessionUsername = HttpContext.Session.GetString("username");
            string? sessionToken = HttpContext.Session.GetString("token");
            // string? sessionId = HttpContext.Session.GetString("session_id"); // Optional: Retrieve session ID if needed for stricter checks

            // Retrieve values from Cookies
            string? cookieUsername = Request.Cookies["username"];
            string? cookieToken = Request.Cookies["token"];
            // string? cookieSessionId = Request.Cookies["session_id"]; // Optional: Retrieve session ID

            // Validate that both session and cookie values exist and match
            if (string.IsNullOrEmpty(sessionUsername) || string.IsNullOrEmpty(sessionToken) ||
                string.IsNullOrEmpty(cookieUsername) || string.IsNullOrEmpty(cookieToken) ||
                sessionUsername != cookieUsername || sessionToken != cookieToken)
                // Optional stricter check: || sessionId != cookieSessionId
            {
                // If validation fails, redirect to the Login page
                // Ensure the path "/Login" correctly points to your login page.
                return RedirectToPage("/Login");
            }
            // --- END ACCESS CONTROL CHECK ---

            // --- Original OnGet Logic Starts Here ---
            // If the code reaches here, the user is considered authenticated.
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

            // Since the method now returns IActionResult, we must return Page() at the end
            // if we are not redirecting.
            return Page();
            // --- End of Original OnGet Logic ---
        }

        // --- OnPost Methods (Add, Delete, Edit remain unchanged) ---
         // It's good practice to add access control to POST handlers as well,
         // especially if they modify data. You can add the same check at the beginning
         // of OnPostAdd, OnPostDelete, OnPostEdit if needed.

         public IActionResult OnPostAdd()
         {
             // *** Optional: Add Access Control Check here too ***
             // if (!IsUserAuthenticated()) return RedirectToPage("/Login");

             ModelState.Remove("SearchClassName");
             ModelState.Remove("MinStudentCount");
             ModelState.Remove("MaxStudentCount");

             if (!ModelState.IsValid)
             {
                  // Call OnGet logic again to repopulate necessary data for the view
                  // Since OnGet now returns IActionResult, we need to handle its potential redirect
                  var getResult = OnGet();
                  if (getResult is RedirectToPageResult) return getResult; // Propagate redirect if auth failed in OnGet
                  return Page(); // Otherwise, just return the page with validation errors
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
             // *** Optional: Add Access Control Check here too ***
             // if (!IsUserAuthenticated()) return RedirectToPage("/Login");

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
              // This is a helper method, no direct access control needed here.
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
             // *** Optional: Add Access Control Check here too ***
             // if (!IsUserAuthenticated()) return RedirectToPage("/Login");

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
                 // Optionally, handle the case where the item to edit is not found
                 // return NotFound(); // Or redirect with an error message
             }

             // Call OnGet logic again to repopulate the page correctly for display
             var getResult = OnGet(); // This will also perform the auth check
             if (getResult is RedirectToPageResult) return getResult; // Propagate redirect if auth failed

             return Page(); // Return the page to show the edit form
         }


         // --- Handler for JSON Export ---
         // *** IMPORTANT: Add Access Control Check here too! Exporting data should be protected. ***
         public IActionResult OnGetExportJson(
             [FromQuery] string exportMode,
             [FromQuery] string[]? selectedColumns,
             [FromQuery] string? searchClassName,
             [FromQuery] int? minStudentCount,
             [FromQuery] int? maxStudentCount,
             [FromQuery] int? currentPage
            )
         {
             // --- ACCESS CONTROL CHECK ---
             // Retrieve values from Session
             string? sessionUsername = HttpContext.Session.GetString("username");
             string? sessionToken = HttpContext.Session.GetString("token");
             // Retrieve values from Cookies
             string? cookieUsername = Request.Cookies["username"];
             string? cookieToken = Request.Cookies["token"];

             // Validate that both session and cookie values exist and match
             if (string.IsNullOrEmpty(sessionUsername) || string.IsNullOrEmpty(sessionToken) ||
                 string.IsNullOrEmpty(cookieUsername) || string.IsNullOrEmpty(cookieToken) ||
                 sessionUsername != cookieUsername || sessionToken != cookieToken)
             {
                 // If validation fails, return Unauthorized or redirect to Login
                 // Returning Unauthorized (401) might be more appropriate for an API-like endpoint
                 // return Unauthorized();
                 // Or redirect to login page:
                 return RedirectToPage("/Login");
             }
             // --- END ACCESS CONTROL CHECK ---


             // --- Original Export Logic Starts Here ---
             IEnumerable<ClassInformationModel> dataToExport;
             string effectiveMode = exportMode?.ToLowerInvariant() ?? "all";

             switch (effectiveMode)
             {
                 case "currentpage":
                     var queryPage = GetFilteredQuery(searchClassName, minStudentCount, maxStudentCount);
                     int pageNum = Math.Max(1, currentPage.GetValueOrDefault(1));
                     dataToExport = queryPage
                                        .OrderBy(c => c.Id)
                                        .Skip((pageNum - 1) * PageSize)
                                        .Take(PageSize)
                                        .ToList();
                     break;

                 case "filtered":
                     dataToExport = GetFilteredQuery(searchClassName, minStudentCount, maxStudentCount)
                                        .OrderBy(c => c.Id)
                                        .ToList();
                     break;

                 case "all":
                 default:
                     InitializeData();
                     dataToExport = _allClassList
                                        .OrderBy(c => c.Id)
                                        .ToList();
                     break;
             }

             string jsonString;
             if (selectedColumns != null && selectedColumns.Length > 0)
             {
                 var projectedData = dataToExport.Select(item =>
                 {
                     var dict = new Dictionary<string, object>();
                     foreach (var colName in selectedColumns)
                     {
                         // Using case-insensitive comparison for robustness
                         if (string.Equals(colName, nameof(ClassInformationModel.Id), StringComparison.OrdinalIgnoreCase))
                             dict[colName] = item.Id;
                         else if (string.Equals(colName, nameof(ClassInformationModel.ClassName), StringComparison.OrdinalIgnoreCase))
                             dict[colName] = item.ClassName;
                         else if (string.Equals(colName, nameof(ClassInformationModel.StudentCount), StringComparison.OrdinalIgnoreCase))
                             dict[colName] = item.StudentCount;
                         else if (string.Equals(colName, nameof(ClassInformationModel.Description), StringComparison.OrdinalIgnoreCase))
                             dict[colName] = item.Description ?? string.Empty;
                         // Add other properties here if they can be selected
                     }
                     return dict;
                 }).ToList();
                 jsonString = Utils.Instance.ConvertToJsonString<Dictionary<string, object>>(projectedData, prettyPrint: true);
             }
             else
             {
                 jsonString = Utils.Instance.ConvertToJsonString<ClassInformationModel>(dataToExport, prettyPrint: true);
             }

             if (jsonString.Contains("error\":\"Failed to serialize data"))
             {
                 Console.Error.WriteLine($"Export failed: Serialization error detected by Utils class for mode '{exportMode}'.");
                 TempData["ExportError"] = "Could not generate the JSON file due to an internal error.";
                 return RedirectToPage(new { currentPage = this.CurrentPage, SearchClassName = searchClassName, MinStudentCount = minStudentCount, MaxStudentCount = maxStudentCount });
             }

             var fileName = $"class_export_{effectiveMode}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
             return File(Encoding.UTF8.GetBytes(jsonString), "application/json", fileName);
             // --- End of Original Export Logic ---
         }

         // Optional Helper method for checking authentication to avoid repetition
         // private bool IsUserAuthenticated()
         // {
         //     string? sessionUsername = HttpContext.Session.GetString("username");
         //     string? sessionToken = HttpContext.Session.GetString("token");
         //     string? cookieUsername = Request.Cookies["username"];
         //     string? cookieToken = Request.Cookies["token"];
         //
         //     return !string.IsNullOrEmpty(sessionUsername) && !string.IsNullOrEmpty(sessionToken) &&
         //            !string.IsNullOrEmpty(cookieUsername) && !string.IsNullOrEmpty(cookieToken) &&
         //            sessionUsername == cookieUsername && sessionToken == cookieToken;
         // }
    }
}
