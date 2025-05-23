using System.Collections.Generic;
using WebApp.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApp.ViewModels
{
    public class ClassDetailsViewModel
    {
        public List<Class> Classes { get; set; }
        public List<Student> AllStudents { get; set; }
        public List<AppUser> AllTeachers { get; set; }

        public string ClassName { get; set; }
        public string TeacherId { get; set; }

        public List<Student> AssignedStudents { get; set; }

        public Dictionary<string, List<string>> ClassStudentMap { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public Dictionary<int, string> SubjectTeacherNames { get; set; } = new();
    }
}