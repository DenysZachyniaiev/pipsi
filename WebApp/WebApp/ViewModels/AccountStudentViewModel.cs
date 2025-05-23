using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class AccountStudentViewModel
    {
        public AppUser User { get; set; } = new();
        public Student? Student { get; set; }
        public List<Assignment> Assignments { get; set; } = new();
        public Dictionary<int, int?> Grades { get; set; } = new();
    }
}