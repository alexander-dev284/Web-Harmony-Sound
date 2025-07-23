namespace HarmonySound.API.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string RoleName { get; set; } = "";
    }
    
    public class CreateRoleDto
    {
        public string Name { get; set; } = "";
        public string RoleName { get; set; } = "";
    }
}