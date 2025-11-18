// Models/Branch.cs
using Microsoft.CodeAnalysis.Operations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Branch : BaseEntity
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Branch Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        [StringLength(10)]
        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int? ParentBranchId { get; set; } // For sub-branches

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Department? Department { get; set; }
        public virtual Branch? ParentBranch { get; set; }
        public virtual ICollection<Branch> SubBranches { get; set; } = new List<Branch>();
        public virtual ICollection<Semester> BranchSemesters { get; set; } = new List<Semester>();
        public virtual ICollection<Semester> SubBranchSemesters { get; set; } = new List<Semester>();
        /*
        [NotMapped]
        public string FullPath => ParentBranch != null ?
            $"{Department?.FullPath} → {ParentBranch.Name} → {Name}" :
            $"{Department?.FullPath} → {Name}";
        */

        [NotMapped]
         public bool HasSubBranches => SubBranches?.Any() == true;

        [NotMapped]
        public string FullPath
        {
            get
            {
                var pathParts = new List<string>();

                if (Department != null)
                {
                    if (Department.College != null && Department.College.University != null)
                        pathParts.Add(Department.College.University.Name);
                    if (Department.College != null)
                        pathParts.Add(Department.College.Name);
                    pathParts.Add(Department.Name);
                }

                if (ParentBranch != null)
                    pathParts.Add(ParentBranch.Name);

                pathParts.Add(Name);

                return string.Join(" → ", pathParts);
            }
        }





    }
}