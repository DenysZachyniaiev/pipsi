using WebApp.Models;
namespace WebApp.ViewModels
{
    public class ClassDetailsViewModel
    {
        public List<Class> Classes { get; set; }
        public List<Student> AllStudents { get; set; }
        public List<AppUser> AllTeachers { get; set; }
        public Dictionary<string, List<int>> ClassStudentMap { get; set; } = new();
        public string? ClassName { get; set; }
        public string? TeacherId { get; set; }
        public List<Student>? AssignedStudents { get; set; }
    }
}