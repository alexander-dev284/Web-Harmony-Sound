using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class User
    {
        [Key] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Email { get; set; }
        [Required] public string Password { get; set; }
        [Required] public DateTime RegisterDate { get; set; }
        [Required] public string State { get; set; }


        public List<UserRole> UserRoles { get; set; }
        public List<UserPlan> UserPlans { get; set; }
        public List<Content> Content { get; set; }  // Si es un Artista
        public List<Report> Reports { get; set; }
        public List<SubscriptionHistory> SubscriptionHistory { get; set; }
    }
}
