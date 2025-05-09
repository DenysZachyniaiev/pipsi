using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Student
    {

        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public Student() { }
    }
}
