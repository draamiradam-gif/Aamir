using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class SemestersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SemestersController> _logger;

        public SemestersController(ApplicationDbContext context, ILogger<SemestersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Semesters
        public async Task<IActionResult> Index()
        {
            var semesters = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.StartDate)
                .ToListAsync();

            return View(semesters);
        }

        // GET: Semesters/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var semester = await _context.Semesters
        //        .Include(s => s.Department)
        //        .Include(s => s.Branch)
        //        .Include(s => s.SubBranch)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (semester == null) return NotFound();

        //    return View(semester);
        //}

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        
        // GET: Semesters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            await PopulateDropdowns();
            return View(semester);
        }

        // POST: Semesters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,SemesterType,AcademicYear,DepartmentId,BranchId,SubBranchId,StartDate,EndDate,RegistrationStartDate,RegistrationEndDate,IsActive,IsCurrent,IsRegistrationOpen")] Semester semester)
        {
            if (id != semester.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(semester);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Semester '{semester.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SemesterExists(semester.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(semester);
        }

        // GET: Semesters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (semester == null) return NotFound();

            // Check if there are any courses linked to this semester
            var hasCourses = await _context.Courses.AnyAsync(c => c.SemesterId == id);
            if (hasCourses)
            {
                TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
                return RedirectToAction(nameof(Index));
            }

            return View(semester);
        }

        // POST: Semesters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);

            if (semester == null) return NotFound();

            // Check if there are any courses linked to this semester
            var hasCourses = await _context.Courses.AnyAsync(c => c.SemesterId == id);
            if (hasCourses)
            {
                TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Semester '{semester.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Semesters/Duplicate/5
        public async Task<IActionResult> Duplicate(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (semester == null) return NotFound();

            var viewModel = new DuplicateViewModel
            {
                EntityType = "Semester",
                SourceId = semester.Id,
                NewName = $"{semester.Name} - Copy",
                TargetParentId = semester.DepartmentId,
                AcademicYearOffset = 1 // Default to 1 year offset
            };

            await PopulateDropdowns();
            return View(viewModel);
        }

        // POST: Semesters/Duplicate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id, DuplicateViewModel model)
        {
            if (id != model.SourceId) return NotFound();

            var sourceSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sourceSemester == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Create new semester
                    var newSemester = new Semester
                    {
                        Name = model.NewName,
                        SemesterType = sourceSemester.SemesterType,
                        AcademicYear = sourceSemester.AcademicYear + model.AcademicYearOffset,
                        DepartmentId = model.TargetParentId ?? sourceSemester.DepartmentId,
                        BranchId = sourceSemester.BranchId,
                        SubBranchId = sourceSemester.SubBranchId,
                        StartDate = sourceSemester.StartDate.AddYears(model.AcademicYearOffset),
                        EndDate = sourceSemester.EndDate.AddYears(model.AcademicYearOffset),
                        RegistrationStartDate = sourceSemester.RegistrationStartDate.AddYears(model.AcademicYearOffset),
                        RegistrationEndDate = sourceSemester.RegistrationEndDate.AddYears(model.AcademicYearOffset),
                        IsActive = sourceSemester.IsActive,
                        IsCurrent = false, // Don't copy current semester status
                        IsRegistrationOpen = sourceSemester.IsRegistrationOpen
                    };

                    _context.Semesters.Add(newSemester);
                    await _context.SaveChangesAsync();

                    // Duplicate courses if requested
                    if (model.CopySubItems)
                    {
                        var sourceCourses = await _context.Courses
                            .Where(c => c.SemesterId == id && c.IsActive)
                            .ToListAsync();

                        foreach (var course in sourceCourses)
                        {
                            var newCourse = new Course
                            {
                                CourseCode = $"{course.CourseCode}-COPY",
                                CourseName = course.CourseName,
                                Description = course.Description,
                                Credits = course.Credits,
                                DepartmentId = course.DepartmentId,
                                SemesterId = newSemester.Id,
                                IsActive = course.IsActive,
                                MaxStudents = course.MaxStudents,
                                MinGPA = course.MinGPA,
                                MinPassedHours = course.MinPassedHours
                                // Copy other course properties as needed
                            };
                            _context.Courses.Add(newCourse);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Semester '{sourceSemester.Name}' duplicated successfully as '{model.NewName}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error duplicating semester {SemesterId}", id);
                    ModelState.AddModelError("", "An error occurred while duplicating the semester.");
                }
            }

            await PopulateDropdowns();
            return View(model);
        }

        // Helper methods
        private async Task PopulateDropdowns()
        {
            await PopulateDepartmentsDropdown();
            await PopulateBranchesDropdown();
            await PopulateSubBranchesDropdown();
        }

        private async Task PopulateDepartmentsDropdown()
        {
            var departments = await _context.Departments
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Where(d => d.IsActive)
                .OrderBy(d => d.College!.University!.Name)
                .ThenBy(d => d.College!.Name)
                .ThenBy(d => d.Name)
                .Select(d => new { d.Id, Name = $"{d.College!.University!.Name} → {d.College!.Name} → {d.Name}" })
                .ToListAsync();

            ViewBag.DepartmentId = new SelectList(departments, "Id", "Name");
        }

        private async Task PopulateBranchesDropdown()
        {
            var branches = await _context.Branches
                .Include(b => b.Department)
                .Where(b => b.IsActive && b.ParentBranchId == null)
                .OrderBy(b => b.Department!.Name)
                .ThenBy(b => b.Name)
                .Select(b => new { b.Id, Name = $"{b.Department!.Name} → {b.Name}" })
                .ToListAsync();

            ViewBag.BranchId = new SelectList(branches, "Id", "Name");
        }

        private async Task PopulateSubBranchesDropdown()
        {
            var subBranches = await _context.Branches
                .Include(b => b.Department)
                .Include(b => b.ParentBranch)
                .Where(b => b.IsActive && b.ParentBranchId != null)
                .OrderBy(b => b.Department!.Name)
                .ThenBy(b => b.ParentBranch!.Name)
                .ThenBy(b => b.Name)
                .Select(b => new { b.Id, Name = $"{b.Department!.Name} → {b.ParentBranch!.Name} → {b.Name}" })
                .ToListAsync();

            ViewBag.SubBranchId = new SelectList(subBranches, "Id", "Name");
        }

        private bool SemesterExists(int id)
        {
            return _context.Semesters.Any(e => e.Id == id);
        }

        // GET: Semesters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (semester == null) return NotFound();

            return View(semester);
        }
        //// GET: Semesters/Create with parameters
        //[HttpGet]
        //[Route("Semesters/Create/{departmentId?}/{branchId?}/{subBranchId?}")]
        //public async Task<IActionResult> Create(int? departmentId, int? branchId, int? subBranchId)
        //{
        //    if (departmentId.HasValue)
        //    {
        //        var department = await _context.Departments
        //            .Include(d => d.College!)
        //            .ThenInclude(c => c.University!)
        //            .FirstOrDefaultAsync(d => d.Id == departmentId);

        //        ViewBag.ParentDepartment = department ?? null;
        //        ViewBag.EntityType = department != null ? "Department" : null;
        //    }
        //    else
        //    {
        //        ViewBag.ParentDepartment = null;
        //        ViewBag.EntityType = null;
        //    }

        //    if (branchId.HasValue)
        //    {
        //        var branch = await _context.Branches
        //            .Include(b => b.Department!)
        //            .ThenInclude(d => d.College!)
        //            .ThenInclude(c => c.University!)
        //            .FirstOrDefaultAsync(b => b.Id == branchId);

        //        ViewBag.ParentBranch = branch ?? null;
        //        ViewBag.EntityType = branch != null ? "Branch" : null;
        //    }
        //    else
        //    {
        //        ViewBag.ParentBranch = null;
        //    }

        //    if (subBranchId.HasValue)
        //    {
        //        var subBranch = await _context.Branches
        //            .Include(b => b.Department!)
        //            .ThenInclude(d => d.College!)
        //            .ThenInclude(c => c.University!)
        //            .Include(b => b.ParentBranch)
        //            .FirstOrDefaultAsync(b => b.Id == subBranchId);

        //        ViewBag.ParentSubBranch = subBranch ?? null;
        //        ViewBag.EntityType = subBranch != null ? "Sub-Branch" : null;
        //    }
        //    else
        //    {
        //        ViewBag.ParentSubBranch = null;
        //    }

        //    await PopulateDropdowns();
        //    return View("Create");
        //}


        // POST: Semesters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SemesterType,AcademicYear,DepartmentId,BranchId,SubBranchId,StartDate,EndDate,RegistrationStartDate,RegistrationEndDate,IsActive,IsCurrent,IsRegistrationOpen")] Semester semester)
        {
            if (ModelState.IsValid)
            {
                _context.Add(semester);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Semester '{semester.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(semester);
        }

        /*
         // GET: Semesters/Delete/5
public async Task<IActionResult> Delete(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var semester = await _context.Semesters
        .Include(s => s.Department)
        .Include(s => s.Branch)
        .Include(s => s.SubBranch)
        .Include(s => s.Courses)
        .FirstOrDefaultAsync(m => m.Id == id);

    if (semester == null)
    {
        return NotFound();
    }

    // Check if there are any courses linked to this semester
    if (semester.Courses.Any())
    {
        TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
        return RedirectToAction(nameof(Index));
    }

    return View(semester);
}

// POST: Semesters/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var semester = await _context.Semesters
        .Include(s => s.Courses)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (semester == null)
    {
        return NotFound();
    }

    // Check if there are any courses linked to this semester
    if (semester.Courses.Any())
    {
        TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
        return RedirectToAction(nameof(Index));
    }

    _context.Semesters.Remove(semester);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = $"Semester '{semester.Name}' deleted successfully.";
    return RedirectToAction(nameof(Index));
}
         */
    }
}