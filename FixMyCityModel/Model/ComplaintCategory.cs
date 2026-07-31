using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ComplaintCategory.cs
namespace FixMyCityModel.Model
{
    public class ComplaintCategory
    {
        public int DepartmentId;
        public string DepartmentName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }
}
