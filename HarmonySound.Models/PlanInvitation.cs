using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class PlanInvitation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int InviterId { get; set; } // Usuario que envía la invitación
        public User Inviter { get; set; }
        
        [Required]
        [EmailAddress]
        public string InviteeEmail { get; set; } // Email del invitado
        
        public int? InviteeId { get; set; } // ID del usuario invitado
        public User? Invitee { get; set; }
        
        [Required]
        public int PlanId { get; set; }
        public Plan Plan { get; set; }
        
        [Required]
        public string InvitationToken { get; set; } // Token único para la invitación
        
        public DateTimeOffset InvitedDate { get; set; }
        public DateTimeOffset? AcceptedDate { get; set; }
        public DateTimeOffset ExpirationDate { get; set; }
        
        [Required]
        public string Status { get; set; } 
        
        public string? InvitationMessage { get; set; }
    }
}