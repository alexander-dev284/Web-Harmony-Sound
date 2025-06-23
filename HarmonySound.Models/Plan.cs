using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Plan
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string PlanName { get; set; }  
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int AccountLimit { get; set; }
    }
}
