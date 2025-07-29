namespace HarmonySound.MVC.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalArtists { get; set; }
        public int TotalClients { get; set; }
        public int TotalSongs { get; set; }
        public int TotalAlbums { get; set; }
        public int PendingReports { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    }

    public class RecentActivityViewModel
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
        public string UserName { get; set; } = "";
    }

    public class AdminUserViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string State { get; set; } = "";
        public DateTime RegisterDate { get; set; }
        public string Role { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
    }

    public class AdminContentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string ArtistName { get; set; } = "";
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public string UrlMedia { get; set; } = "";
    }

}