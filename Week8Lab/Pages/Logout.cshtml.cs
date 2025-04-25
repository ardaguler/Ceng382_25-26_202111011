using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http; // Required for HttpContext, Session, Cookies
using System; // Required for DateTime

/*
AI Prompts for Logout.cshtml.cs :

// Prompt: "Create a PageModel for Logout (LogoutModel)."
// Prompt: "How to clear the session in ASP.NET Core?"
// Prompt: "Show C# code to remove specific cookies ('username', 'token', 'session_id')."
// Prompt: "How to redirect to the '/Login' page from a PageModel?"

*/


// Ensure this namespace matches your project structure
namespace Week8Lab.Pages
{
    public class LogoutModel : PageModel
    {
        // This handler will be executed when a GET request is made to /Logout
        public IActionResult OnGet()
        {
            // 1. Clear the session variables
            HttpContext.Session.Clear();

            // 2. Remove the authentication cookies
            // To remove a cookie, we append it again with an expiration date in the past.
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1), // Set expiration to yesterday
                HttpOnly = true,
                Secure = true, // Match the settings used when creating the cookie
                SameSite = SameSiteMode.Strict // Match the settings used when creating the cookie
            };

            // Append cookies with past expiration to effectively delete them
            Response.Cookies.Append("username", "", cookieOptions);
            Response.Cookies.Append("token", "", cookieOptions);
            Response.Cookies.Append("session_id", "", cookieOptions);

            // 3. Redirect the user to the Login page
            return RedirectToPage("/Login"); // Ensure this path is correct
        }
    }
}
