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
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    public class SemestersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SemestersController> _logger;
        private readonly ISemesterService _semesterService;

        public SemestersController(ApplicationDbContext context,
                              ILogger<SemestersController> logger,
                              ISemesterService semesterService) // Add this
        {
            _context = context;
            _logger = logger;
            _semesterService = semesterService; // Add this
        }

        // GET: Semesters
        public async Task<IActionResult> Index(string status, int? year, string type)
        {
            var query = _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(s => s.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(s => !s.IsActive);
                        break;
                    case "current":
                        query = query.Where(s => s.IsCurrent);
                        break;
                    case "registration":
                        query = query.Where(s => s.IsRegistrationOpen);
                        break;
                }
            }

            if (year.HasValue)
            {
                query = query.Where(s => s.AcademicYear == year.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(s => s.SemesterType == type);
            }

            var semesters = await query
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.StartDate)
                .ToListAsync();

            // Pass filter values to maintain state
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentYear = year;
            ViewBag.CurrentType = type;

            return View(semesters);
        }

        private async Task PopulateFilterDropdowns()
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.Departments = new SelectList(departments, "Id", "Name");
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

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            await PopulateDropdowns(); // You may need to create this method
            return View(course);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyExists(int id)
        {
            var exists = await _context.Courses.AnyAsync(c => c.Id == id);
            return Json(exists);
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

            // Pass this to the view
            ViewBag.HasCourses = hasCourses;

            if (hasCourses)
            {
                TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
                return RedirectToAction(nameof(Index));
            }

            return View(semester);
        }

        // Add the missing actions using service
        //[HttpPost]
        //public async Task<IActionResult> SetCurrent(int id)
        //{
        //    var result = await _semesterService.SetCurrentSemesterAsync(id);
        //    if (result)
        //    {
        //        var semester = await _context.Semesters.FindAsync(id);
        //        TempData["SuccessMessage"] = $"Semester '{semester?.Name}' set as current semester.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "Error setting current semester.";
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

        //[HttpPost]
        //public async Task<IActionResult> CloseRegistration(int id)
        //{
        //    var result = await _semesterService.CloseRegistrationAsync(id);
        //    if (result)
        //    {
        //        var semester = await _context.Semesters.FindAsync(id);
        //        TempData["SuccessMessage"] = $"Registration closed for semester '{semester?.Name}'.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "Error closing registration.";
        //    }

        //    return RedirectToAction(nameof(Details), new { id });
        //}

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
                        DepartmentId = model.TargetParentId ?? sourceSemester.DepartmentId ?? 0,
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
                                DepartmentId = course.DepartmentId ?? 0,
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
        // GET: Semesters/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var semester = await _context.Semesters
        //        .Include(s => s.Department)
        //        .Include(s => s.Branch)
        //        .Include(s => s.SubBranch)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (semester == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(semester);
        //}
        // In SemestersController Details action, add this:
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (semester == null) return NotFound();

            // Load courses directly instead of using AJAX
            var courses = await _context.Courses
                .Where(c => c.SemesterId == id && c.IsActive)
                .ToListAsync();

            ViewBag.Courses = courses;

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


        // Add these methods to SemestersController.cs

        [HttpPost]
        public async Task<IActionResult> SetCurrent(int id)
        {
            try
            {
                // Clear current flag from all semesters
                var currentSemesters = await _context.Semesters.Where(s => s.IsCurrent).ToListAsync();
                foreach (var semester in currentSemesters)
                {
                    semester.IsCurrent = false;
                }

                // Set the selected semester as current
                var targetSemester = await _context.Semesters.FindAsync(id);
                if (targetSemester != null)
                {
                    targetSemester.IsCurrent = true;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Semester '{targetSemester.Name}' set as current semester.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting current semester {SemesterId}", id);
                TempData["ErrorMessage"] = "Error setting current semester.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CloseRegistration(int id)
        {
            try
            {
                var semester = await _context.Semesters.FindAsync(id);
                if (semester != null)
                {
                    semester.IsRegistrationOpen = false;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Registration closed for semester '{semester.Name}'.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing registration for semester {SemesterId}", id);
                TempData["ErrorMessage"] = "Error closing registration.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Add this to CoursesController.cs
        [HttpGet]
        public async Task<IActionResult> GetCoursesBySemester(int semesterId)
        {
            var courses = await _context.Courses
                .Where(c => c.SemesterId == semesterId && c.IsActive)
                .Select(c => new
                {
                    id = c.Id,
                    courseName = c.CourseName,
                    courseCode = c.CourseCode,
                    credits = c.Credits,
                    description = c.Description,
                    isActive = c.IsActive
                })
                .ToListAsync();

            return Json(courses);
        }


        // TEMPORARY: Debug action to check semester data
        public async Task<IActionResult> Debug()
        {
            var semesters = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .ToListAsync();

            ViewBag.Semesters = semesters;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCourseFromSemester(int semesterId, int courseId)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found in this semester." });
                }

                // Remove the semester association
                course.SemesterId = 0;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Course removed from semester successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing course {CourseId} from semester {SemesterId}", courseId, semesterId);
                return Json(new { success = false, message = "An error occurred while removing the course." });
            }
        }

        
        // GET: Semesters/ManageCourse/5?semesterId=4
        public async Task<IActionResult> ManageCourse(int? id, int semesterId)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.CourseSemester)
                .FirstOrDefaultAsync(c => c.Id == id && c.SemesterId == semesterId);

            if (course == null) return NotFound();

            ViewBag.SemesterId = semesterId;
            return View(course);
        }

        // POST: Semesters/UpdateCourseInSemester
        [HttpPost]
        public async Task<IActionResult> UpdateCourseInSemester(int courseId, int semesterId, Course courseUpdate)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

            if (course == null) return NotFound();

            // Update course properties as needed
            course.CourseCode = courseUpdate.CourseCode;
            course.CourseName = courseUpdate.CourseName;
            // ... other properties

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course updated successfully.";

            return RedirectToAction("Details", new { id = semesterId });
        }

        //////
        ///
        // TEMPORARY: Debug action to check what's working
        public async Task<IActionResult> DebugAll()
        {
            var semesters = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .OrderBy(s => s.Id)
                .ToListAsync();

            ViewBag.Semesters = semesters;

            // Check if Details action exists
            try
            {
                var detailsAction = typeof(SemestersController).GetMethod("Details");
                ViewBag.DetailsActionExists = detailsAction != null;
            }
            catch
            {
                ViewBag.DetailsActionExists = false;
            }

            // Check if Details view exists
            var viewPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Semesters", "Details.cshtml");
            ViewBag.DetailsViewExists = System.IO.File.Exists(viewPath);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCoursesForReplacement(int semesterId, int currentCourseId)
        {
            var availableCourses = await _context.Courses
                .Where(c => c.SemesterId == semesterId) // Courses not assigned or in same semester
                .Where(c => c.Id != currentCourseId) // Exclude current course
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    id = c.Id,
                    courseCode = c.CourseCode,
                    courseName = c.CourseName
                })
                .ToListAsync();

            return Json(availableCourses);
        }

        [HttpPost]
        public async Task<IActionResult> ReplaceCourseInSemester(int semesterId, int currentCourseId, int replacementCourseId, bool keepOriginalCourse)
        {
            try
            {
                var currentCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == currentCourseId && c.SemesterId == semesterId);

                var replacementCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == replacementCourseId);

                if (currentCourse == null || replacementCourse == null)
                {
                    return Json(new { success = false, message = "Course not found." });
                }

                // Update replacement course to this semester
                replacementCourse.SemesterId = semesterId;

                // Handle original course
                if (!keepOriginalCourse)
                {
                    // Remove original course from semester (keep in system)
                    currentCourse.SemesterId = 0;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Course replaced successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing course {CurrentCourseId} with {ReplacementCourseId} in semester {SemesterId}",
                    currentCourseId, replacementCourseId, semesterId);
                return Json(new { success = false, message = "An error occurred while replacing the course." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSemesters(int currentSemesterId)
        {
            var availableSemesters = await _context.Semesters
                .Where(s => s.Id != currentSemesterId) // Exclude current semester
                .Where(s => s.IsActive) // Only active semesters
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.StartDate)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    academicYear = s.AcademicYear,
                    semesterType = s.SemesterType
                })
                .ToListAsync();

            return Json(availableSemesters);
        }


        [HttpPost]
        public async Task<IActionResult> MoveCourseToSemester(int courseId, int fromSemesterId, int toSemesterId)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == fromSemesterId);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found in source semester." });
                }

                // Check if target semester exists
                var targetSemester = await _context.Semesters.FindAsync(toSemesterId);
                if (targetSemester == null)
                {
                    return Json(new { success = false, message = "Target semester not found." });
                }

                // Move the course to new semester
                course.SemesterId = toSemesterId;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Course moved to new semester successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving course {CourseId} from semester {FromSemesterId} to {ToSemesterId}",
                    courseId, fromSemesterId, toSemesterId);
                return Json(new { success = false, message = "An error occurred while moving the course." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddCourseToSemester(int semesterId, int courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found." });
                }

                // Check if course is already in this semester
                if (course.SemesterId == semesterId)
                {
                    return Json(new { success = false, message = "Course is already in this semester." });
                }

                // Add course to semester
                course.SemesterId = semesterId;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Course added to semester successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding course {CourseId} to semester {SemesterId}", courseId, semesterId);
                return Json(new { success = false, message = "An error occurred while adding the course." });
            }
        }
    }
}