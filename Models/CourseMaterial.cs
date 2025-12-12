using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class CourseMaterial : BaseEntity
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Material Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Document"; // Document, Video, Assignment, Textbook, etc.

        [Required]
        [StringLength(500)]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Original File Name")]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "File Path")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "File Size")]
        public long FileSize { get; set; }

        [Required]
        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "Uploaded By")]
        public string UploadedBy { get; set; } = string.Empty;

        [Display(Name = "Visible to Students")]
        public bool IsVisibleToStudents { get; set; } = true;

        [Display(Name = "Allow Download")]
        public bool AllowDownload { get; set; } = true;

        [Display(Name = "View Online Only")]
        public bool ViewOnlineOnly { get; set; } = false;

        [StringLength(100)]
        [Display(Name = "Access Level")]
        public string AccessLevel { get; set; } = "All"; // All, Registered, SpecificGroups

        // Navigation property
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        // Computed properties
        [NotMapped]
        public string FileSizeFormatted => FormatFileSize(FileSize);

        [NotMapped]
        public string TypeIcon => GetTypeIcon();

        [NotMapped]
        public string FileExtension => Path.GetExtension(OriginalFileName).ToLower();

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetTypeIcon()
        {
            return FileExtension switch
            {
                ".pdf" => "fas fa-file-pdf text-danger",
                ".doc" or ".docx" => "fas fa-file-word text-primary",
                ".ppt" or ".pptx" => "fas fa-file-powerpoint text-warning",
                ".xls" or ".xlsx" => "fas fa-file-excel text-success",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "fas fa-file-image text-info",
                ".mp4" or ".avi" or ".mov" => "fas fa-file-video text-secondary",
                ".zip" or ".rar" => "fas fa-file-archive text-warning",
                ".txt" => "fas fa-file-alt text-muted",
                _ => "fas fa-file text-secondary"
            };
        }

        public bool CanUserAccess(bool isStudent, bool isEnrolledInCourse)
        {
            if (!IsVisibleToStudents) return false;

            return AccessLevel switch
            {
                "All" => true,
                "Registered" => isStudent && isEnrolledInCourse,
                "SpecificGroups" => false, // Would need additional logic for groups
                _ => true
            };
        }
    }
}