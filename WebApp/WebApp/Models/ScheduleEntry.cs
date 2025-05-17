using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class ScheduleEntry
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public int SubjectId { get; set; }

        public string ClassroomNumber { get; set; }
    }
}