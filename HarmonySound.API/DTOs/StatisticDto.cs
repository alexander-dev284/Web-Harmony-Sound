namespace HarmonySound.API.DTOs
{
    public class StatisticDto
    {
        public int ContentId { get; set; }
        public string Title { get; set; }

        public int Reproductions { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
        public DateTimeOffset ReportDate { get; set; }
    }
}
