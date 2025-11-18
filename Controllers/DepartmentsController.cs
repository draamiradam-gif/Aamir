// Controllers/DepartmentsController.cs
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using DuplicateViewModel = StudentManagementSystem.Models.ViewModels.DuplicateViewModel;




namespace StudentManagementSystem.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(ApplicationDbContext context, ILogger<DepartmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Include(d => d.Branches)
                .Include(d => d.Semesters)
                .Where(d => d.IsActive)
                .OrderBy(d => d.College != null && d.College.University != null ? d.College.University.Name : "")
                .ThenBy(d => d.College != null ? d.College.Name : "")
                .ThenBy(d => d.Name)
                .ToListAsync();

            return View(departments);
        }

        
        // GET: Departments/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var department = await _context.Departments
        //        .Include(d => d.College!)
        //            .ThenInclude(c => c.University!)
        //        .Include(d => d.Branches)
        //        .Include(d => d.Semesters)
        //        .Include(d => d.Courses)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (department == null) return NotFound();

        //    return View(department);
        //}

        // GET: Departments/Create
        //public async Task<IActionResult> Create()
        //{
        //    await PopulateCollegesDropdown();
        //    return View();
        //}

        

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            await PopulateCollegesDropdown();
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DepartmentCode,Description,CollegeId,StartYear,IsMajorDepartment,MinimumGPAMajor,TotalBenches,AvailableBenches,IsActive")] Department department)
        {
            if (id != department.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if department name already exists in the same college (excluding current)
                    var existingDepartment = await _context.Departments
                        .AnyAsync(d => d.CollegeId == department.CollegeId &&
                                      d.Name == department.Name &&
                                      d.Id != id &&
                                      d.IsActive);

                    if (existingDepartment)
                    {
                        ModelState.AddModelError("Name", "A department with this name already exists in the selected college.");
                        await PopulateCollegesDropdown();
                        return View(department);
                    }

                    _context.Update(department);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Department '{department.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateCollegesDropdown();
            return View(department);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.College)
                .Include(d => d.Branches)
                .Include(d => d.Semesters)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department == null) return NotFound();

            if (department.Branches.Any() || department.Semesters.Any() || department.Courses.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete department because it has associated branches, semesters, or courses. Delete them first.";
                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Branches)
                .Include(d => d.Semesters)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return NotFound();

            if (department.Branches.Any() || department.Semesters.Any() || department.Courses.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete department because it has associated branches, semesters, or courses. Delete them first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Department '{department.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Departments/Duplicate/5
        public async Task<IActionResult> Duplicate(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.College)
                .Include(d => d.Branches)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return NotFound();

            var viewModel = new DuplicateViewModel
            {
                EntityType = "Department",
                SourceId = department.Id,
                NewName = $"{department.Name} - Copy",
                TargetParentId = department.CollegeId
            };

            await PopulateCollegesDropdown();
            return View(viewModel);
        }

        // POST: Departments/Duplicate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id, DuplicateViewModel model)
        {
            if (id != model.SourceId) return NotFound();

            var sourceDepartment = await _context.Departments
                .Include(d => d.Branches)
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (sourceDepartment == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Create new department
                    var newDepartment = new Department
                    {
                        Name = model.NewName,
                        DepartmentCode = $"{sourceDepartment.DepartmentCode}-COPY",
                        Description = sourceDepartment.Description,
                        CollegeId = model.TargetParentId ?? sourceDepartment.CollegeId,
                        StartYear = sourceDepartment.StartYear,
                        IsMajorDepartment = sourceDepartment.IsMajorDepartment,
                        MinimumGPAMajor = sourceDepartment.MinimumGPAMajor,
                        TotalBenches = sourceDepartment.TotalBenches,
                        AvailableBenches = sourceDepartment.AvailableBenches,
                        IsActive = sourceDepartment.IsActive
                    };

                    _context.Departments.Add(newDepartment);
                    await _context.SaveChangesAsync();

                    // Duplicate branches if requested
                    if (model.CopySubItems && sourceDepartment.Branches.Any())
                    {
                        foreach (var branch in sourceDepartment.Branches.Where(b => b.IsActive))
                        {
                            var newBranch = new Branch
                            {
                                Name = branch.Name,
                                BranchCode = $"{branch.BranchCode}-COPY",
                                Description = branch.Description,
                                DepartmentId = newDepartment.Id,
                                ParentBranchId = branch.ParentBranchId,
                                IsActive = branch.IsActive
                            };
                            _context.Branches.Add(newBranch);
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Duplicate courses if requested
                    if (model.CopySubItems && sourceDepartment.Courses.Any())
                    {
                        foreach (var course in sourceDepartment.Courses.Where(c => c.IsActive))
                        {
                            var newCourse = new Course
                            {
                                CourseCode = $"{course.CourseCode}-COPY",
                                CourseName = course.CourseName,
                                Description = course.Description,
                                Credits = course.Credits,
                                Department = course.Department,
                                Semester = course.Semester,
                                IsActive = course.IsActive,
                                MaxStudents = course.MaxStudents,
                                MinGPA = course.MinGPA,
                                MinPassedHours = course.MinPassedHours,
                                DepartmentId = newDepartment.Id,
                                SemesterId = course.SemesterId
                            };
                            _context.Courses.Add(newCourse);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Department '{sourceDepartment.Name}' duplicated successfully as '{model.NewName}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error duplicating department {DepartmentId}", id);
                    ModelState.AddModelError("", "An error occurred while duplicating the department.");
                }
            }

            await PopulateCollegesDropdown();
            return View(model);
        }

        // GET: Departments/ManageBranches/5
        public async Task<IActionResult> ManageBranches(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Branches)
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return NotFound();

            var viewModel = new DepartmentBranchesViewModel
            {
                Department = department,
                NewBranch = new Branch { DepartmentId = department.Id }
            };

            return View(viewModel);
        }

        // POST: Departments/AddBranch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBranch([Bind("Name,BranchCode,Description,DepartmentId,IsActive")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                // Check if branch name already exists in the same department
                var existingBranch = await _context.Branches
                    .AnyAsync(b => b.DepartmentId == branch.DepartmentId &&
                                  b.Name == branch.Name &&
                                  b.IsActive);

                if (existingBranch)
                {
                    TempData["ErrorMessage"] = "A branch with this name already exists in this department.";
                    return RedirectToAction("ManageBranches", new { id = branch.DepartmentId });
                }

                _context.Add(branch);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Branch '{branch.Name}' added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid branch data.";
            }

            return RedirectToAction("ManageBranches", new { id = branch.DepartmentId });
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }

        

        public class DepartmentBranchesViewModel
        {
            public Department Department { get; set; } = null!;
            public Branch NewBranch { get; set; } = null!;
        }
              

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Include(d => d.Branches)
                    .ThenInclude(b => b.SubBranches)
                .Include(d => d.Students)
                .Include(d => d.Courses)
                .Include(d => d.Semesters)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department == null) return NotFound();

            return View(department);
        }
        // GET: Departments/Create
        public async Task<IActionResult> Create(int? collegeId)
        {
            // Initialize ViewBag properties to avoid null references
            ViewBag.ParentCollege = null;
            ViewBag.CollegeId = new SelectList(new List<object>(), "Id", "Name");

            if (collegeId.HasValue)
            {
                var college = await _context.Colleges
                    .Include(c => c.University)
                    .FirstOrDefaultAsync(c => c.Id == collegeId);

                if (college != null)
                {
                    ViewBag.ParentCollege = college;

                    // Pre-select the college in dropdown
                    var colleges = await _context.Colleges
                        .Include(c => c.University)
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.University != null ? c.University.Name : "")
                        .ThenBy(c => c.Name)
                        .Select(c => new {
                            c.Id,
                            Name = $"{(c.University != null ? c.University.Name : "")} - {c.Name}"
                        })
                        .ToListAsync();

                    ViewBag.CollegeId = new SelectList(colleges, "Id", "Name", collegeId.Value);
                }
            }

            // If no collegeId or college not found, populate dropdown normally
            if (ViewBag.CollegeId == null || ((SelectList)ViewBag.CollegeId).Count() == 0)
            {
                await PopulateCollegesDropdown();
            }

            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DepartmentCode,Description,CollegeId,StartYear,IsMajorDepartment,MinimumGPAMajor,TotalBenches,AvailableBenches,IsActive")] Department department)
        {
            if (ModelState.IsValid)
            {
                // Check if department name already exists in the same college
                var existingDepartment = await _context.Departments
                    .AnyAsync(d => d.CollegeId == department.CollegeId &&
                                  d.Name == department.Name &&
                                  d.IsActive);

                if (existingDepartment)
                {
                    ModelState.AddModelError("Name", "A department with this name already exists in the selected college.");
                    await PopulateCollegesDropdown();
                    return View(department);
                }

                _context.Add(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Department '{department.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCollegesDropdown();
            return View(department);
        }

        private async Task PopulateCollegesDropdown()
        {
            var colleges = await _context.Colleges
                .Include(c => c.University)
                .Where(c => c.IsActive)
                .OrderBy(c => c.University != null ? c.University.Name : "")
                .ThenBy(c => c.Name)
                .Select(c => new {
                    c.Id,
                    Name = $"{(c.University != null ? c.University.Name : "")} - {c.Name}"
                })
                .ToListAsync();

            ViewBag.CollegeId = new SelectList(colleges, "Id", "Name");
        }





    }

        
            
    }