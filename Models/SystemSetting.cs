namespace StudentManagementSystem.Models
{
    public class SystemSettings
    {
        public int Id { get; set; }

        public bool AllowDropAfterDeadline { get; set; } = false;
        public bool AllowDropWithGrade { get; set; } = false;


    }

    
}
