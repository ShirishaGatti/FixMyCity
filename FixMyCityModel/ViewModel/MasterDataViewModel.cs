using FixMyCityModel.Model;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    // Composite VM for Views/Admin/MasterData.cshtml. Only the currently
    // active partial's lookups need be populated; the others stay empty and
    // are loaded lazily via JSON endpoints if the user opens that card.
    public class MasterDataViewModel
    {
        public List<State> States { get; set; } = new List<State>();
        public List<District> Districts { get; set; } = new List<District>();
        public List<City> Cities { get; set; } = new List<City>();
        public List<Ward> Wards { get; set; } = new List<Ward>();
        public List<ComplaintCategory> Categories { get; set; } = new List<ComplaintCategory>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Role> Roles { get; set; } = new List<Role>();

    }

    // Single POST DTO for every master save (State/District/City/Ward/Category/Department).
    // EntityType decides which subset of fields is required — the service
    // layer validates against that, keeping controller code trivial.
    public class MasterEntitySaveViewModel
    {
        // "State" | "District" | "City" | "Ward" | "Category" | "Department"
        public string EntityType { get; set; }

        public int Id { get; set; } // 0 = insert, >0 = update
        public string Name { get; set; }
        public int? ParentId { get; set; } // District→StateId, City→DistrictId, Ward→CityId
        public bool IsActive { get; set; } = true;
        public string WardNo { get; set; }
        public int DepartmentId { get; set; }
    }
     public class MasterEntityViewModel
   {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public string ParentName { get; set; }
        public bool IsActive { get; set; }
        public string WardNo { get; set; }
    }
}
