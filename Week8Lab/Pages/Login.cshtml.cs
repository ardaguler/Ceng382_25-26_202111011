using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations; // Required for [Required], [BindProperty]
using System.Threading.Tasks; // Required for Task
using System.IO; // Required for Path, File
using System.Text.Json; // Required for JsonSerializer
using System.Linq; // Required for LINQ methods like FirstOrDefault
using System; // Required for Guid, DateTime
using Week8Lab.Models; // Required for the User model
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using Microsoft.AspNetCore.Http; // Required for HttpContext, Session, Cookies, CookieOptions

/*
AI Prompts for Login.cshtml.cs :

// Prompt: "Create a PageModel for Login with bind properties for Username and Password."
// Prompt: "How to read a 'users.json' file from wwwroot in a PageModel?"
// Prompt: "Write C# code to find a user in a list based on username and check password/active status."
// Prompt: "How to set session variables ('username', 'token', 'session_id') after successful login?"
// Prompt: "Show how to set cookies ('username', 'token', 'session_id') with HttpOnly, Secure, SameSite, and expiration."
// Prompt: "How to redirect to '/Index' page after successful login?"
// Prompt: "How to show an error message if login fails?"

*/

// Ensure this namespace matches your project structure
namespace Week8Lab.Pages
{
    public class LoginModel : PageModel
    {
        // Service to get information about the web hosting environment (e.g., paths)
        private readonly IWebHostEnvironment _environment;

        // Constructor to inject the IWebHostEnvironment service
        public LoginModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Property to bind the username input from the form
        [BindProperty]
        [Required(ErrorMessage = "Username is required.")] // Basic validation
        public string InputUsername { get; set; } = string.Empty;

        // Property to bind the password input from the form
        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)] // Tells the framework this is a password field
        public string InputPassword { get; set; } = string.Empty;

        // Property to display error messages on the login page
        // Using a simple property is fine if not using TempData
        public string? ErrorMessage { get; set; } // Nullable string

        // This method handles GET requests to the page.
        public void OnGet()
        {
            // Potential future enhancement: Check if user is already logged in via session/cookie
            // string? sessionToken = HttpContext.Session.GetString("token");
            // string? cookieToken = Request.Cookies["token"];
            // if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(cookieToken) && sessionToken == cookieToken)
            // {
            //     // Optionally redirect to Index if already logged in
            //     // Response.Redirect("/Index"); // Or use RedirectToPage("/Index") if within an async method context
            // }
        }

        // This method handles POST requests (when the login form is submitted).
        public async Task<IActionResult> OnPostAsync()
        {
            // Check if the submitted form data is valid based on the [Required] attributes
            if (!ModelState.IsValid)
            {
                return Page(); // Redisplay the page with validation errors
            }

            // --- ACTUAL LOGIN LOGIC ---

            // 1. Construct the path to the users.json file
            // Combines the web root path (wwwroot) with the relative path to the file
            var jsonFilePath = Path.Combine(_environment.WebRootPath, "data", "users.json");

            // Check if the JSON file exists
            if (!System.IO.File.Exists(jsonFilePath))
            {
                ErrorMessage = "Configuration error: User data file not found.";
                return Page(); // Show error and stop processing
            }

            try
            {
                // 2. Read the JSON file content
                var jsonString = await System.IO.File.ReadAllTextAsync(jsonFilePath);

                // 3. Deserialize the JSON string into a list of User objects
                // If the file is empty or invalid JSON, this might return null or throw an exception
                var users = JsonSerializer.Deserialize<List<User>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Allows matching JSON properties like "username" to C# properties like "Username"
                });

                // Check if deserialization was successful and we have a list
                if (users == null)
                {
                    ErrorMessage = "Configuration error: Could not read user data.";
                    return Page();
                }

                // 4. Find the user matching the submitted username (case-insensitive comparison)
                var foundUser = users.FirstOrDefault(u => u.Username.Equals(InputUsername, StringComparison.OrdinalIgnoreCase));

                // 5. Validate the user
                if (foundUser != null && foundUser.Password == InputPassword && foundUser.IsActive)
                {
                    // --- Successful Login ---

                    // a. Generate a simple unique token (e.g., a GUID)
                    var token = Guid.NewGuid().ToString();
                    // b. Get the current session ID
                    var sessionId = HttpContext.Session.Id; // Get the unique ID for the current session

                    // c. Store information in the session
                    HttpContext.Session.SetString("username", foundUser.Username);
                    HttpContext.Session.SetString("token", token);
                    HttpContext.Session.SetString("session_id", sessionId); // Store session ID in session itself for potential cross-check

                    // d. Define cookie options as per requirements
                    var cookieOptions = new CookieOptions
                    {
                        // Cookie expires in 30 minutes from now
                        Expires = DateTime.UtcNow.AddMinutes(30),
                        // Cookie is not accessible via client-side script (important for security)
                        HttpOnly = true,
                        // Cookie should only be sent over HTTPS
                        Secure = true, // Set to true if your site uses HTTPS (recommended)
                        // Cookie should only be sent on requests originating from the same site
                        SameSite = SameSiteMode.Strict // Strongest protection against CSRF
                    };

                    // e. Store the same information in cookies
                    Response.Cookies.Append("username", foundUser.Username, cookieOptions);
                    Response.Cookies.Append("token", token, cookieOptions);
                    Response.Cookies.Append("session_id", sessionId, cookieOptions); // Store session ID in cookie

                    // f. Redirect to the main application page (e.g., the class list)
                    return RedirectToPage("/Index"); // Redirects to Index.cshtml
                }
                else
                {
                    // --- Failed Login ---
                    // User not found, password incorrect, or user is inactive
                    ErrorMessage = "Invalid username or password.";
                    return Page(); // Redisplay the login page with the error message
                }
            }
            catch (JsonException jsonEx)
            {
                // Handle errors during JSON deserialization
                ErrorMessage = $"Configuration error: Invalid format in user data file. {jsonEx.Message}";
                // Log the detailed exception jsonEx for debugging
                Console.Error.WriteLine($"JSON Deserialization Error: {jsonEx}");
                return Page();
            }
            catch (Exception ex)
            {
                // Handle other potential errors (e.g., file reading issues)
                ErrorMessage = "An unexpected error occurred during login.";
                // Log the detailed exception ex for debugging
                Console.Error.WriteLine($"Login Error: {ex}");
                return Page();
            }
        }
    }
}
