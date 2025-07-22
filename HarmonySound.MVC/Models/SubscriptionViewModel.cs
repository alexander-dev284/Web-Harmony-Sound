using HarmonySound.Models;
namespace HarmonySound.MVC.Models
{
    public class SubscriptionViewModel
    {
        public Plan? CurrentPlan { get; set; }
        public UserPlan? CurrentUserPlan { get; set; } // ✅ NUEVO
        public List<Plan> PremiumPlans { get; set; } = new List<Plan>();
    }
}
