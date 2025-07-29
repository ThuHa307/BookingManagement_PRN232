using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.ChatBot
{
    public class PostWithCommentsDto
    {
        public string PostContent { get; set; }
        public List<string> CommentContents { get; set; }
    }
}