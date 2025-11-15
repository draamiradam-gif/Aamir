using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class BranchSubBranchesViewModel
    {
        public Branch ParentBranch { get; set; } = null!;
        public Branch NewSubBranch { get; set; } = null!;
    }
}