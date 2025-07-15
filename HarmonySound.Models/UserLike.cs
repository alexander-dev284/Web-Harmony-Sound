using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class UserLike
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        public int ContentId { get; set; }
        public Content Content { get; set; }
        
        public DateTimeOffset LikeDate { get; set; }
    }
}