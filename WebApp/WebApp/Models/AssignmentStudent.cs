namespace WebApp.Models
{
    public class AssignmentStudent
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public Student Student { get; set; }
    }
}