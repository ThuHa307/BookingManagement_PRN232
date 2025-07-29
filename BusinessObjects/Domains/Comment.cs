using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Domains
{
    public partial class Comment
    {
        public int CommentId { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public int? ParentCommentId { get; set; }

        public string Content { get; set; } = null!;

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; }

        public virtual Account? Account { get; set; } = null!;

        public virtual Post? Post { get; set; } = null!;

        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment>? ChildComments { get; set; } = new List<Comment>();
    }

}
