using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Student
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public Student() { }
    }
}