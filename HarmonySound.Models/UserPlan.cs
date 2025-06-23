using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class UserPlan
    {
        [Key] public int Id { get; set; }

        public int UserId { get; set; }

        public int PlanId { get; set; }
        public User? User { get; set; }
        public Plan? Plan { get; set; }

        public DateTimeOffset  StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public bool Active { get; set; }
    }
}
