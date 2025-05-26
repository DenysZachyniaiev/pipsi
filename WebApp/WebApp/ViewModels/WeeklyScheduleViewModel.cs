namespace WebApp.ViewModels
{
    public class WeeklyScheduleViewModel
    {
        public string ClassName { get; set; } = string.Empty;
        public DateTime WeekStart { get; set; }
        public List<ScheduleEntryDisplay> ScheduleEntries { get; set; } = new();
    }

    public class ScheduleEntryDisplay
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string ClassroomNumber { get; set; } = string.Empty;
    }
}