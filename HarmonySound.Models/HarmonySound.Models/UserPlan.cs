using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class UserPlan
    {
        [Key] public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool Active { get; set; }
    }
}
