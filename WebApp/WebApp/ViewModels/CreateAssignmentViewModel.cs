using System;
using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class CreateAssignmentViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubjectName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string ClassName { get; set; }
        public List<Student> AllStudents { get; set; } = new();
        public List<string> SelectedStudentIds { get; set; } = new();
    }
}