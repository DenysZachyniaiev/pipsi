using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class Grade
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string SubjectName { get; set; } = string.Empty;

        [Range(1, 6)]
        public int Value { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public int? AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }
    }
}
