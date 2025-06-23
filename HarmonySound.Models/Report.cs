using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Report
    {
        [Key] public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Description { get; set; }

        public string ReportType { get; set; }

        public DateTimeOffset ReportDate { get; set; }

        public string State { get; set; }  // Ejemplo: "Pendiente", "Resuelto"
    }
}
