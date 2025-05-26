using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class ScheduleEntry
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }
        [Range(8, 16, ErrorMessage = "Hour must be between 8 and 16.")]
        public int Hour { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public int SubjectId { get; set; }

        public string ClassroomNumber { get; set; }
    }
}