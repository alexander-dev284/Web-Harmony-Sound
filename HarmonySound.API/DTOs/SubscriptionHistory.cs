namespace HarmonySound.API.DTOs
{
    public class SubscriptionHistory
    {
        public int UserId { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string State { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
