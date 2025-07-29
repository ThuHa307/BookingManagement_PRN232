namespace WebMVC.Models
{
    public class AdminPostViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string StatusDisplayName => Status switch
        {
            "P" => "Chờ duyệt",
            "A" => "Đã duyệt",
            "R" => "Bị từ chối",
            "E" => "Hết hạn",
            "C" => "Bị hủy",
            _ => "Không xác định"
        };

        public string StatusBadgeClass => Status switch
        {
            "P" => "badge-warning",
            "A" => "badge-success",
            "R" => "badge-danger",
            "E" => "badge-secondary",
            "C" => "badge-danger",
            _ => "badge-light"
        };
    }

    public class AdminPostDetailViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? AverageScore { get; set; }
        public int CommentsCount { get; set; }
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();

        public string StatusDisplayName => Status switch
        {
            "P" => "Chờ duyệt",
            "A" => "Đã duyệt",
            "R" => "Bị từ chối",
            "E" => "Hết hạn",
            "C" => "Bị hủy",
            _ => "Không xác định"
        };

        public string ScoreBackgroundClass => AverageScore switch
        {
            >= 7 => "bg-success",
            < 7  => "bg-danger",
            _ => "bg-secondary"
        };

        public string ScoreTextClass => AverageScore switch
        {
            >= 7 => "text-white",
            < 7 => "text-white",
            _ => "text-white"
        };
    }

    public class CommentViewModel
    {
        public int CommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
