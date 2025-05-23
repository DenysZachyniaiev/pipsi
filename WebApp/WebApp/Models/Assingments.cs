using WebApp.Models;

public class Assignment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;

    public string? ClassName { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }

    public ICollection<AssignmentStudent> AssignedStudents { get; set; } = new List<AssignmentStudent>();
}
