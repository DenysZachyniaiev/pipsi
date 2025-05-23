using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class ManageGradesViewModel
    {
        public string ClassName { get; set; }

        public List<Student> Students { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
        public Dictionary<int, Dictionary<string, int?>> Grades { get; set; } = new();
        public Dictionary<int, HashSet<string>> AssignmentStudentMap { get; set; } = new();
        public DateTime? WeekStart { get; set; }
    }
}