using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class ImportReviewViewModel
    {
        //public required ImportResult ImportResult { get; set; }
        //public required ImportSettings ImportSettings { get; set; }
        
        //public string? SearchString { get; set; }

        public ImportResult ImportResult { get; set; } = new ImportResult();
        public ImportSettings ImportSettings { get; set; } = new ImportSettings();
        public string SortBy { get; set; } = "SerialNumber";
        public string SortOrder { get; set; } = "asc";
        public string SearchString { get; set; } = string.Empty;
        public string SearchType { get; set; } = "All";
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
  
    }
}