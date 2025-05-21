using System;
using System.Collections.Generic;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class CreateAssignmentViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; } // np. Sprawdzian, Kartkówka, Zadanie domowe
        public string SubjectName { get; set; }

        public DateTime StartDate { get; set; } // wcześniej: Date
        public DateTime? DueDate { get; set; }  // tylko jeśli potrzebne

        public string ClassName { get; set; }

        public string Test = "test";

        public List<Student> AllStudents { get; set; } = new();
        public List<int> SelectedStudentIds { get; set; } = new();
    }
}