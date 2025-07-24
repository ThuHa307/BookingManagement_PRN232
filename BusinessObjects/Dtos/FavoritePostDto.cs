using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos
{

    public class FavoritePostDto
    {
        public int FavoriteId { get; set; }
        public int PostId { get; set; }
        public int AccountId { get; set; }
    }

}
