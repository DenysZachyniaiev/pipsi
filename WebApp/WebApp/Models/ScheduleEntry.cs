namespace WebApp.Models
{
    public class ScheduleEntry
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }

        public int SubjectId { get; set; }

        public string ClassroomNumber { get; set; }
    }
}