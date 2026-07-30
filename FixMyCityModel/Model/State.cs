using System;

namespace FixMyCityModel.Model
{
    // Maps FixMyCity.State — top of the State → District → City → Ward hierarchy.
    public class State
    {
        public int StateId { get; set; }
        public string StateName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
