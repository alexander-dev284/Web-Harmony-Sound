namespace HarmonySound.MVC.Models
{
    public class InvitationDto
    {
        public int Id { get; set; }
        public string InviteeEmail { get; set; }
        public string? InviteeName { get; set; }
        public string PlanName { get; set; }
        public string Status { get; set; }
        public DateTimeOffset InvitedDate { get; set; }
        public DateTimeOffset? AcceptedDate { get; set; }
        public DateTimeOffset ExpirationDate { get; set; }

    }
}
