using System.Collections.Generic;
using Core.Cate.Models;

namespace Modules.Sys.Areas.Sys.Data
{
    public class UserBoPhanModel
    {
        public int? UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string MaBophans { get; set; }
        public string BoPhanIDs { get; set; }
        public List<MN_BoPhanModel> ListBophans { get; set; } = new List<MN_BoPhanModel>();
    }
}
