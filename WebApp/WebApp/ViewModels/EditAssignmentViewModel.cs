using System;
using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class EditAssignmentViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Type { get; set; }
        public string SubjectName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string ClassName { get; set; }

        public List<Subject> AvailableSubjects { get; set; } = new();

        public List<Student> AllStudents { get; set; } = new();
        public List<int> SelectedStudentIds { get; set; } = new();
    }
}