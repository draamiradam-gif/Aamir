using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // These are the columns that exist in your database
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Map these to existing columns using [NotMapped] and computed properties
        [NotMapped]
        public DateTime CreatedAt
        {
            get => CreatedDate;
            set => CreatedDate = value;
        }

        [NotMapped]
        public DateTime UpdatedAt
        {
            get => ModifiedDate ?? CreatedDate;
            set => ModifiedDate = value;
        }

        [NotMapped]
        public string? CreatedBy { get; set; } = "System";

        [NotMapped]
        public string? UpdatedBy { get; set; } = "System";

        [NotMapped]
        public bool IsDeleted { get; set; } = false;

        // Method to update timestamps
        public void UpdateTimestamps(string updatedBy = "System")
        {
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy;
            ModifiedDate = DateTime.Now;

            if (Id == 0) // New entity
            {
                CreatedAt = DateTime.Now;
                CreatedDate = DateTime.Now;
                CreatedBy = updatedBy;
            }
        }
    }
}