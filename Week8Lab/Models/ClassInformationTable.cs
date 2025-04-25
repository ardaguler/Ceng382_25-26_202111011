// Models/ClassInformationTable.cs
namespace Week8Lab.Models // Use your actual project's namespace
{
    /// <summary>
    /// Represents the data structure specifically for display in the class list table.
    /// It includes the ID needed for actions, even though the ID might not be rendered visually in the table.
    /// </summary>
    public class ClassInformationTable
    {
        public int Id { get; set; } // ID is needed for Edit/Delete actions
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string? Description { get; set; } // Make nullable if it can be empty
    }
}