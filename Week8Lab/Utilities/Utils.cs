// Utils.cs
using System;
using System.Collections.Generic;
using System.Text.Json; // Requires System.Text.Json

// Prompt 1 (Singleton):
// "Create a thread-safe Singleton pattern in C# for a utility class named 'Utils'."

// Prompt 2 (JSON Method Core):
// "Write a generic C# method ConvertToJsonString<T> that takes an IEnumerable<T> and serializes it to JSON using System.Text.Json."

namespace Week8Lab.Utilities // Or Week8Lab.Utilities if you created a subfolder
{
    /// <summary>
    /// Singleton utility class providing common helper methods.
    /// </summary>
    public sealed class Utils
    {
        // --- Singleton Implementation ---
        private static readonly Lazy<Utils> lazyInstance =
            new Lazy<Utils>(() => new Utils()); // Thread-safe lazy initialization

        public static Utils Instance => lazyInstance.Value; // Public access point

        private Utils() // Private constructor prevents external instantiation
        {
            // Initialization logic for the utility class, if any needed
        }
        // --- End Singleton Implementation ---


        // --- Generic JSON Conversion Method ---

        /// <summary>
        /// Converts a collection of any type T into a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of objects in the collection.</typeparam>
        /// <param name="data">The collection of data to serialize.</param>
        /// <param name="prettyPrint">If true, formats the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the data, or "[]" if data is null.</returns>
        public string ConvertToJsonString<T>(IEnumerable<T>? data, bool prettyPrint = true)
        {
            if (data == null)
            {
                return "[]"; // Return empty JSON array for null input
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = prettyPrint,
                    // Add other options if needed, e.g.:
                    // PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // Converters = { new JsonStringEnumConverter() }
                };

                return JsonSerializer.Serialize(data, options);
            }
            catch (Exception ex)
            {
                // Log the exception (using a proper logging framework is recommended)
                Console.Error.WriteLine($"Error serializing data to JSON: {ex.Message}");
                // Depending on requirements, re-throw, return null, or return an error indicator
                return "{\"error\":\"Failed to serialize data\"}";
            }
        }
        // --- End Generic JSON Conversion Method ---

        // Add other utility methods here if needed in the future...
    }
}