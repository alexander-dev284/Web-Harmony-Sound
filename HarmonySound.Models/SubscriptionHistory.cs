using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class SubscriptionHistory
    {
        [Key] public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        public DateTimeOffset TransactionDate { get; set; }

        public decimal Amount { get; set; }

        public string State { get; set; }  // Ejemplo: "Éxito", "Fallido"

        public string PaymentMethod { get; set; }  // "PayPhone"

        public string PayReference { get; set; }

        public DateTimeOffset? ExpirationDate { get; set; }
    }
}
