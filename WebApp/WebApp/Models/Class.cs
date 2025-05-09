using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApp.Models;

namespace WebApp.Models
{
    public class Class
    {
        [Key]
        public string Name { get; set; } = string.Empty;

        public string TeacherId { get; set; } = string.Empty;
    }
}
