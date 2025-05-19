namespace WebApp.Models
{
    public class AssignmentStudent
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }
    }
}