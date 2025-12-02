using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StudentManagementSystem.Controllers
{
    public class SemestersController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SemestersController> _logger;
        private readonly ISemesterService _semesterService;
        private readonly IEnrollmentService _enrollmentService;

        public SemestersController(ApplicationDbContext context,
                              ILogger<SemestersController> logger,
                              ISemesterService semesterService,
                              IEnrollmentService enrollmentService) // Add this
        {
            _context = context;
            _logger = logger;
            _semesterService = semesterService; // Add this
            _enrollmentService = enrollmentService;
            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
        }

        // GET: Semesters
        public async Task<IActionResult> Index(string status, int? year, string type)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
                
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            await PopulateDropdowns();
            return View();
        }


        // GET: Semesters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id == null) return NotFound();

            // FIX: Get Semester instead of Course
            var semester = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (semester == null) return NotFound();

            await PopulateDropdowns();
            return View(semester);
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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var semester = await _context.Semesters
        //        .Include(s => s.Department)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (semester == null) return NotFound();

        //    // Check if there are any courses linked to this semester
        //    var hasCourses = await _context.Courses.AnyAsync(c => c.SemesterId == id);

        //    // Pass this to the view
        //    ViewBag.HasCourses = hasCourses;

        //    if (hasCourses)
        //    {
        //        TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(semester);
        //}



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
        //// POST: Semesters/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var semester = await _context.Semesters.FindAsync(id);

        //    if (semester == null) return NotFound();

        //    // Check if there are any courses linked to this semester
        //    var hasCourses = await _context.Courses.AnyAsync(c => c.SemesterId == id);
        //    if (hasCourses)
        //    {
        //        TempData["ErrorMessage"] = "Cannot delete semester because it has associated courses. Delete the courses first.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    _context.Semesters.Remove(semester);
        //    await _context.SaveChangesAsync();

        //    TempData["SuccessMessage"] = $"Semester '{semester.Name}' deleted successfully.";
        //    return RedirectToAction(nameof(Index));
        //}

        // GET: Semesters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (semester == null) return NotFound();

            // FIX: Check if there are any courses linked to this semester using direct query
            var hasCourses = await _context.Courses
                .AnyAsync(c => c.SemesterId == id);

            // Pass this to the view
            ViewBag.HasCourses = hasCourses;

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var semester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.Id == id);

            if (semester == null) return NotFound();

            // FIX: Check if there are any courses linked to this semester using direct query
            var hasCourses = await _context.Courses
                .AnyAsync(c => c.SemesterId == id);

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



        // GET: Semesters/Duplicate/5
        public async Task<IActionResult> Duplicate(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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



        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var semester = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (semester == null) return NotFound();

            // Load courses for this semester
            var courses = await _context.Courses
                .Where(c => c.SemesterId == id && c.IsActive)
                .Include(c => c.CourseEnrollments)
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .ToListAsync();

            // Ensure ViewBag.Courses is never null
            ViewBag.Courses = courses ?? new List<Course>();
            ViewBag.SemesterId = id;

            return View(semester);
        }


        // POST: Semesters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SemesterType,AcademicYear,DepartmentId,BranchId,SubBranchId,StartDate,EndDate,RegistrationStartDate,RegistrationEndDate,IsActive,IsCurrent,IsRegistrationOpen")] Semester semester)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.SemesterId == semesterId && c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department ?? "No Department",
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        isActive = c.IsActive
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
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

        //[HttpPost]
        //public async Task<IActionResult> RemoveCourseFromSemester(int semesterId, int courseId)
        //{
        //    try
        //    {
        //        var course = await _context.Courses
        //            .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

        //        if (course == null)
        //        {
        //            return Json(new { success = false, message = "Course not found in this semester." });
        //        }

        //        // Remove the semester association by setting to null
        //        course.SemesterId = null;
        //        await _context.SaveChangesAsync();

        //        return Json(new { success = true, message = "Course removed from semester successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error removing course {CourseId} from semester {SemesterId}", courseId, semesterId);
        //        return Json(new { success = false, message = "Error removing course from semester." });
        //    }
        //}


        // GET: Semesters/ManageCourse/5?semesterId=4
        public async Task<IActionResult> ManageCourse(int? id, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            var viewPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Views", "Semesters", "Details.cshtml");
            ViewBag.DetailsViewExists = System.IO.File.Exists(viewPath);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCoursesForReplacement(int semesterId, int currentCourseId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

        /////////////
        ///

        // Add to SemestersController.cs

        [HttpPost]
        public async Task<IActionResult> RemoveCoursesFromSemester([FromBody] RemoveCoursesRequest request)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (request == null || !request.CourseIds.Any())
                {
                    return Json(new { success = false, message = "No courses selected." });
                }

                var courses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id) && c.SemesterId == request.SemesterId)
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.SemesterId = null; // Use null since SemesterId is nullable
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{courses.Count} course(s) removed from semester successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing courses from semester {SemesterId}", request?.SemesterId ?? 0);
                return Json(new { success = false, message = "An error occurred while removing courses." });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> RemoveCourseFromSemester(int semesterId, int courseId)
        //{
        //    try
        //    {
        //        var course = await _context.Courses
        //            .Include(c => c.CourseEnrollments)
        //            .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

        //        if (course == null)
        //        {
        //            return Json(new { success = false, message = "Course not found in this semester." });
        //        }

        //        // Check if there are active enrollments
        //        var activeEnrollments = course.CourseEnrollments?
        //            .Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;

        //        if (activeEnrollments > 0)
        //        {
        //            // Auto-unenroll all students first
        //            var enrollments = await _context.CourseEnrollments
        //                .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
        //                .ToListAsync();

        //            foreach (var enrollment in enrollments)
        //            {
        //                enrollment.IsActive = false;
        //                enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
        //                enrollment.ModifiedDate = DateTime.Now;
        //            }

        //            await _context.SaveChangesAsync();
        //        }

        //        // Now remove the semester association
        //        course.SemesterId = null;
        //        await _context.SaveChangesAsync();

        //        return Json(new
        //        {
        //            success = true,
        //            message = $"Course removed from semester successfully. {activeEnrollments} students were automatically unenrolled."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error removing course {CourseId} from semester {SemesterId}", courseId, semesterId);
        //        return Json(new
        //        {
        //            success = false,
        //            message = $"Error removing course: {ex.Message}"
        //        });
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> RemoveCourseFromSemester(int semesterId, int courseId, bool autoUnenroll = true)
        //{
        //    try
        //    {
        //        var course = await _context.Courses
        //            .Include(c => c.CourseEnrollments)
        //            .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

        //        if (course == null)
        //        {
        //            return Json(new { success = false, message = "Course not found in this semester." });
        //        }

        //        // Check if there are active enrollments
        //        var activeEnrollments = course.CourseEnrollments?
        //            .Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;

        //        if (activeEnrollments > 0 && autoUnenroll)
        //        {
        //            // Auto-unenroll all students first
        //            var enrollments = await _context.CourseEnrollments
        //                .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
        //                .ToListAsync();

        //            foreach (var enrollment in enrollments)
        //            {
        //                enrollment.IsActive = false;
        //                enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
        //                enrollment.ModifiedDate = DateTime.Now;
        //            }
        //            await _context.SaveChangesAsync();
        //        }
        //        else if (activeEnrollments > 0 && !autoUnenroll)
        //        {
        //            return Json(new
        //            {
        //                success = false,
        //                message = $"Cannot remove course. There are {activeEnrollments} active enrollments.",
        //                hasEnrollments = true,
        //                enrollmentCount = activeEnrollments
        //            });
        //        }

        //        // Remove the semester association - use 0 instead of null to avoid foreign key issues
        //        course.SemesterId = 0; // Use 0 instead of null
        //        await _context.SaveChangesAsync();

        //        return Json(new
        //        {
        //            success = true,
        //            message = $"Course removed from semester successfully." +
        //                     (activeEnrollments > 0 ? $" {activeEnrollments} students were auto-unenrolled." : "")
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error removing course {CourseId} from semester {SemesterId}", courseId, semesterId);

        //        // More detailed error information
        //        var errorMessage = "Error removing course from semester.";
        //        if (ex.InnerException != null)
        //        {
        //            errorMessage += $" Details: {ex.InnerException.Message}";
        //        }

        //        return Json(new
        //        {
        //            success = false,
        //            message = errorMessage
        //        });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> RemoveCourseFromSemester(int semesterId, int courseId, bool autoUnenroll = true)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseEnrollments)
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found in this semester." });
                }

                // Handle enrollments if any exist
                var activeEnrollments = course.CourseEnrollments?
                    .Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;

                if (activeEnrollments > 0 && autoUnenroll)
                {
                    var enrollments = await _context.CourseEnrollments
                        .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
                        .ToListAsync();

                    foreach (var enrollment in enrollments)
                    {
                        enrollment.IsActive = false;
                        enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                        enrollment.ModifiedDate = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();
                }

                // Try to remove the semester association
                try
                {
                    course.SemesterId = null;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    // If setting to null fails, use workaround
                    _logger.LogWarning(dbEx, "Cannot set SemesterId to null, using workaround");

                    // Workaround: Use a special semester ID or mark as inactive
                    course.SemesterId = 0; // Or some other value that means "unassigned"
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Course removed from semester (workaround applied). {activeEnrollments} students were auto-unenrolled.",
                        usedWorkaround = true
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Course removed from semester successfully. {activeEnrollments} students were auto-unenrolled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing course {CourseId} from semester {SemesterId}", courseId, semesterId);

                return Json(new
                {
                    success = false,
                    message = $"Error removing course: {ex.InnerException?.Message ?? ex.Message}"
                });
            }
        }

        // First, ensure there's an "Unassigned" semester in your database
        private async Task EnsureUnassignedSemesterExists()
        {
            var unassignedSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.Name == "Unassigned");

            if (unassignedSemester == null)
            {
                unassignedSemester = new Semester
                {
                    Name = "Unassigned",
                    SemesterType = "Other",
                    AcademicYear = DateTime.Now.Year,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(10),
                    RegistrationStartDate = DateTime.Now,
                    RegistrationEndDate = DateTime.Now.AddYears(10),
                    IsActive = true,
                    IsCurrent = false,
                    IsRegistrationOpen = false
                };
                _context.Semesters.Add(unassignedSemester);
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseEnrollments(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                // Validate inputs
                if (courseId <= 0 || semesterId <= 0)
                {
                    return Json(new { error = "Invalid course or semester ID" });
                }

                var enrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Student)
                    .Where(ce => ce.CourseId == courseId &&
                                ce.SemesterId == semesterId &&
                                ce.IsActive)
                    .Select(ce => new
                    {
                        id = ce.Id,
                        studentId = ce.StudentId,
                        studentCode = ce.Student != null ? ce.Student.StudentId : "N/A",
                        studentName = ce.Student != null ? ce.Student.Name : "Unknown Student",
                        status = ce.EnrollmentStatus.ToString(),
                        enrollmentDate = ce.CreatedDate
                    })
                    .ToListAsync();

                return Json(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollments for course {CourseId} in semester {SemesterId}", courseId, semesterId);
                return Json(new { error = $"Error loading enrollments: {ex.Message}" });
            }
        }

        // In SemestersController.cs - Fix the GetAvailableCourses method
        [HttpGet]
        public async Task<IActionResult> GetAvailableCourses(int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var courses = await _context.Courses
                    .Where(c => c.IsActive && (c.SemesterId == 0 || c.SemesterId == semesterId))
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department ?? "No Department",
                        currentSemester = c.SemesterId == semesterId ? "Current" :
                                        (c.SemesterId > 0 ? $"Semester {c.SemesterId}" : "Not assigned"),
                        isActive = c.IsActive,
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active)
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }


        // ADD THIS HELPER METHOD TO THE SEMESTERSCONTROLLER CLASS
        private string GetCurrentSemesterStatus(Course course, int targetSemesterId)
        {
            if (course.SemesterId == targetSemesterId)
                return "Current";

            if (course.SemesterId > 0)
                return course.CourseSemester?.Name ?? $"Semester {course.SemesterId}";

            return "Not assigned";
        }

        [HttpPost]
        public async Task<IActionResult> AddCoursesToSemester([FromBody] AddCoursesRequest request)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (request == null || !request.CourseIds.Any())
                {
                    return Json(new { success = false, message = "No courses selected." });
                }

                var courses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id))
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.SemesterId = request.SemesterId;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{courses.Count} course(s) added to semester successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding courses to semester {SemesterId}", request?.SemesterId ?? 0);
                return Json(new { success = false, message = "An error occurred while adding courses." });
            }
        }


        // In SemestersController.cs - Fix the BulkEnrollStudents method
        [HttpPost]
        public async Task<IActionResult> BulkEnrollStudents([FromBody] BulkEnrollRequest request)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request." });
                }

                var result = await _enrollmentService.BulkEnrollInCoursesAsync(
                    request.SemesterId,
                    request.CourseIds,
                    request.StudentIds
                );

                return Json(new
                {
                    success = true,
                    message = $"Bulk enrollment completed. {result.SuccessfullyEnrolled} students enrolled.",
                    details = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk enrollment for semester {SemesterId}", request?.SemesterId ?? 0);
                return Json(new { success = false, message = "An error occurred during bulk enrollment." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAllCoursesFromSemester([FromBody] int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var courses = await _context.Courses
                    .Where(c => c.SemesterId == semesterId)
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.SemesterId = 0;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"All {courses.Count} courses removed from semester successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all courses from semester {SemesterId}", semesterId);
                return Json(new { success = false, message = "An error occurred while removing all courses." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnrollStudentInCourse(int semesterId, int courseId, int studentId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var result = await _enrollmentService.QuickEnrollInCourseAsync(studentId, courseId, semesterId);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
                return Json(new { success = false, message = "An error occurred during enrollment." });
            }
        }


        // In SemestersController.cs - Add this method
        [HttpGet]
        public async Task<IActionResult> GetStudentEnrollmentOptions(int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var semester = await _context.Semesters.FindAsync(semesterId);
                IQueryable<Student> query = _context.Students.Where(s => s.IsActive);

                if (semester != null && semester.DepartmentId.HasValue)
                {
                    query = query.Where(s => s.DepartmentId == semester.DepartmentId.Value);
                }

                var students = await query
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        code = s.StudentId,
                        gpa = s.GPA.ToString("0.00"),
                        department = s.Department
                    })
                    .ToListAsync();

                return Json(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student enrollment options for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }


        //private async Task<List<Student>> GetStudentsForEnrollment(int semesterId, string selectionCriteria)
        //{
        //    var semester = await _context.Semesters.FindAsync(semesterId);

        //    IQueryable<Student> query = _context.Students.Where(s => s.IsActive);

        //    // FIX: Add null check for semester.DepartmentId
        //    if (semester?.DepartmentId.HasValue == true)
        //    {
        //        query = query.Where(s => s.DepartmentId == semester.DepartmentId.Value);
        //    }

        //    if (selectionCriteria == "eligible")
        //    {
        //        // Additional filtering for eligible students only
        //        // This would check GPA, passed hours, etc.
        //        query = query.Where(s => s.GPA >= 2.0m); // Example: Minimum GPA requirement
        //    }

        //    return await query.ToListAsync();
        //}

        private async Task<List<Student>> GetStudentsForEnrollment(int semesterId, string selectionCriteria)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);

            IQueryable<Student> query = _context.Students.Where(s => s.IsActive);

            // FIX: Proper null checking
            if (semester != null && semester.DepartmentId.HasValue)
            {
                query = query.Where(s => s.DepartmentId == semester.DepartmentId.Value);
            }

            if (selectionCriteria == "eligible")
            {
                // Additional filtering for eligible students only
                query = query.Where(s => s.GPA >= 2.0m); // Example: Minimum GPA requirement
            }

            return await query.ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCoursesForSemester(int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                // ✅ CREDIT HOUR SYSTEM: Show ALL active courses regardless of planned semester
                var availableCourses = await _context.Courses
                    .Where(c => c.IsActive) // Remove: && c.SemesterId != semesterId
                    .Include(c => c.CourseSemester)
                    .Include(c => c.CourseEnrollments)
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department ?? "No Department",
                        plannedSemesterName = c.CourseSemester != null ? c.CourseSemester.Name : "Not planned",
                        plannedSemesterId = c.SemesterId, // This is the PLANNED semester
                        isActive = c.IsActive,
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        status = "Available" // All courses are available in credit hour system
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {availableCourses.Count} available courses for semester {semesterId}");

                return Json(availableCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> AddCoursesToSemester(int semesterId, int[] courseIds)
        //{
        //    try
        //    {
        //        var semester = await _context.Semesters.FindAsync(semesterId);
        //        if (semester == null)
        //        {
        //            return Json(new { success = false, message = "Semester not found." });
        //        }

        //        int addedCount = 0;
        //        foreach (var courseId in courseIds)
        //        {
        //            var course = await _context.Courses.FindAsync(courseId);
        //            if (course != null)
        //            {
        //                course.SemesterId = semesterId;
        //                addedCount++;
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        return Json(new { success = true, addedCount = addedCount });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error adding courses to semester {SemesterId}", semesterId);
        //        return Json(new { success = false, message = "Error adding courses to semester." });
        //    }
        //}

        // Temporary debug method - add this to SemestersController
        [HttpGet]
        public async Task<IActionResult> DebugCourses(int semesterId)
        {
            var allCourses = await _context.Courses
                .Include(c => c.CourseSemester)
                .Select(c => new
                {
                    id = c.Id,
                    courseCode = c.CourseCode,
                    courseName = c.CourseName,
                    semesterId = c.SemesterId,
                    semesterName = c.CourseSemester != null ? c.CourseSemester.Name : "None",
                    isActive = c.IsActive
                })
                .ToListAsync();

            return Json(new
            {
                TotalCourses = allCourses.Count,
                ActiveCourses = allCourses.Count(c => c.isActive),
                CoursesInTargetSemester = allCourses.Count(c => c.semesterId == semesterId),
                AvailableCourses = allCourses.Count(c => c.isActive && c.semesterId != semesterId),
                AllCourses = allCourses
            });
        }

        [HttpGet]
        public async Task<IActionResult> DebugEnrollments()
        {
            var enrollmentStats = await _context.CourseEnrollments
                .GroupBy(ce => ce.EnrollmentStatus)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    ActiveCount = g.Count(ce => ce.IsActive)
                })
                .ToListAsync();

            return Json(new
            {
                TotalEnrollments = await _context.CourseEnrollments.CountAsync(),
                TotalActiveEnrollments = await _context.CourseEnrollments.CountAsync(ce => ce.IsActive),
                StatusBreakdown = enrollmentStats
            });
        }

        // GET: Unenroll student from course (for admin)
        [HttpPost]
        public async Task<IActionResult> UnenrollStudent(int enrollmentId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var enrollment = await _context.CourseEnrollments
                    .Include(ce => ce.Course)
                    .Include(ce => ce.Student)
                    .FirstOrDefaultAsync(ce => ce.Id == enrollmentId && ce.IsActive);

                if (enrollment == null)
                {
                    return Json(new { success = false, message = "Enrollment not found or already inactive." });
                }

                enrollment.IsActive = false;
                enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                enrollment.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Student {enrollment.Student?.Name} unenrolled from {enrollment.Course?.CourseName} successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling student from enrollment {EnrollmentId}", enrollmentId);
                return Json(new { success = false, message = "Error unenrolling student." });
            }
        }

        // GET: Bulk unenroll all students from a course
        [HttpPost]
        public async Task<IActionResult> UnenrollAllFromCourse(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var enrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Course)
                    .Include(ce => ce.Student)
                    .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
                    .ToListAsync();

                if (!enrollments.Any())
                {
                    return Json(new { success = false, message = "No active enrollments found for this course." });
                }

                foreach (var enrollment in enrollments)
                {
                    enrollment.IsActive = false;
                    enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                    enrollment.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{enrollments.Count} students unenrolled from course successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling all students from course {CourseId}", courseId);
                return Json(new { success = false, message = "Error unenrolling students." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseEnrollmentCount(int courseId, int semesterId)
        {
            try
            {
                var enrollmentCount = await _context.CourseEnrollments
                    .CountAsync(ce => ce.CourseId == courseId &&
                                     ce.SemesterId == semesterId &&
                                     ce.IsActive &&
                                     ce.EnrollmentStatus == EnrollmentStatus.Active);

                return Json(new { enrollmentCount = enrollmentCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment count for course {CourseId}", courseId);
                return Json(new { enrollmentCount = 0 });
            }
        }


        /////////
        ///
        [HttpGet]
        public async Task<IActionResult> DebugCourse(int courseId, int semesterId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseEnrollments)
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.SemesterId == semesterId);

                if (course == null)
                {
                    return Json(new { error = "Course not found in semester" });
                }

                var enrollments = await _context.CourseEnrollments
                    .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId)
                    .ToListAsync();

                return Json(new
                {
                    courseId = course.Id,
                    courseName = course.CourseName,
                    semesterId = course.SemesterId,
                    enrollmentCount = enrollments.Count,
                    activeEnrollments = enrollments.Count(e => e.IsActive),
                    enrollments = enrollments.Select(e => new
                    {
                        e.Id,
                        e.StudentId,
                        e.IsActive,
                        e.EnrollmentStatus
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugSemesterCourses(int semesterId)
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.SemesterId == semesterId)
                    .Select(c => new
                    {
                        c.Id,
                        c.CourseCode,
                        c.CourseName,
                        c.SemesterId
                    })
                    .ToListAsync();

                return Json(new
                {
                    semesterId = semesterId,
                    courseCount = courses.Count,
                    courses = courses
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        ////////
        ///
        [HttpPost]
        public async Task<IActionResult> ExportCourses(int semesterId, string courseIds, string exportType)
        {
            try
            {
                var semester = await _context.Semesters
                    .Include(s => s.Department)
                    .FirstOrDefaultAsync(s => s.Id == semesterId);

                if (semester == null)
                {
                    return Json(new { success = false, message = "Semester not found." });
                }

                IQueryable<Course> coursesQuery = _context.Courses
                    .Include(c => c.CourseDepartment)
                    .Include(c => c.CourseEnrollments)
                    .Where(c => c.SemesterId == semesterId && c.IsActive);

                // Filter by selected course IDs if provided
                if (!string.IsNullOrEmpty(courseIds) && courseIds != "all")
                {
                    var selectedCourseIds = courseIds.Split(',').Select(int.Parse).ToList();
                    coursesQuery = coursesQuery.Where(c => selectedCourseIds.Contains(c.Id));
                }

                var courses = await coursesQuery.ToListAsync();

                if (exportType == "excel")
                {
                    return ExportCoursesToExcel(courses, semester);
                }
                else if (exportType == "csv")
                {
                    return ExportCoursesToCsv(courses, semester);
                }

                return Json(new { success = false, message = "Invalid export type." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting courses for semester {SemesterId}", semesterId);
                return Json(new { success = false, message = "Error exporting courses data." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportSemesterData(int semesterId, string exportType)
        {
            try
            {
                var semester = await _context.Semesters
                    .Include(s => s.Department)
                    .Include(s => s.Branch)
                    .Include(s => s.SubBranch)
                    .FirstOrDefaultAsync(s => s.Id == semesterId);

                if (semester == null)
                {
                    return Json(new { success = false, message = "Semester not found." });
                }

                var courses = await _context.Courses
                    .Include(c => c.CourseDepartment)
                    .Include(c => c.CourseEnrollments)
                    .Where(c => c.SemesterId == semesterId && c.IsActive)
                    .ToListAsync();

                var enrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Student)
                    .Include(ce => ce.Course)
                    .Where(ce => ce.SemesterId == semesterId && ce.IsActive)
                    .ToListAsync();

                if (exportType == "summary")
                {
                    return ExportSemesterSummaryToPdf(semester, courses, enrollments);
                }
                else if (exportType == "details")
                {
                    return ExportSemesterDetailsToExcel(semester, courses, enrollments);
                }

                return Json(new { success = false, message = "Invalid export type." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting semester data for semester {SemesterId}", semesterId);
                return Json(new { success = false, message = "Error exporting semester data." });
            }
        }


        private IActionResult ExportCoursesToExcel(List<Course> courses, Semester semester)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Courses");

                // Header
                worksheet.Cells[1, 1].Value = $"Courses for {semester.Name} - {semester.AcademicYear}";
                worksheet.Cells[1, 1, 1, 10].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;

                // Column headers
                var headers = new[] { "Course Code", "Course Name", "Credits", "Department", "Max Students", "Current Enrollment", "Available Seats", "Utilization %", "Status", "Description" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[3, i + 1].Value = headers[i];
                    worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                }

                // Data rows
                int row = 4;
                foreach (var course in courses)
                {
                    var currentEnrollment = course.CourseEnrollments?.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;
                    var availableSeats = course.MaxStudents - currentEnrollment;
                    var utilization = course.MaxStudents > 0 ? (currentEnrollment * 100.0) / course.MaxStudents : 0;

                    worksheet.Cells[row, 1].Value = course.CourseCode;
                    worksheet.Cells[row, 2].Value = course.CourseName;
                    worksheet.Cells[row, 3].Value = course.Credits;
                    worksheet.Cells[row, 4].Value = course.Department ?? "N/A";
                    worksheet.Cells[row, 5].Value = course.MaxStudents;
                    worksheet.Cells[row, 6].Value = currentEnrollment;
                    worksheet.Cells[row, 7].Value = availableSeats;
                    worksheet.Cells[row, 8].Value = Math.Round(utilization, 2);
                    worksheet.Cells[row, 9].Value = course.IsActive ? "Active" : "Inactive";
                    worksheet.Cells[row, 10].Value = course.Description ?? "";
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{semester.Name.Replace(" ", "_")}_Courses_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting courses to Excel");
                TempData["ErrorMessage"] = "Error generating Excel file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportCoursesToCsv(List<Course> courses, Semester semester)
        {
            try
            {
                var csv = new StringBuilder();

                // Header
                csv.AppendLine($"Courses for {semester.Name} - {semester.AcademicYear}");
                csv.AppendLine();

                // Column headers
                csv.AppendLine("Course Code,Course Name,Credits,Department,Max Students,Current Enrollment,Available Seats,Utilization %,Status,Description");

                // Data rows
                foreach (var course in courses)
                {
                    var currentEnrollment = course.CourseEnrollments?.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;
                    var availableSeats = course.MaxStudents - currentEnrollment;
                    var utilization = course.MaxStudents > 0 ? (currentEnrollment * 100.0) / course.MaxStudents : 0;

                    var status = course.IsActive ? "Active" : "Inactive";
                    var description = course.Description?.Replace("\"", "\"\"") ?? "";

                    var line = $"\"{course.CourseCode}\",\"{course.CourseName}\",{course.Credits},\"{course.Department ?? "N/A"}\",{course.MaxStudents},{currentEnrollment},{availableSeats},{Math.Round(utilization, 2)},\"{status}\",\"{description}\"";
                    csv.AppendLine(line);
                }

                var content = Encoding.UTF8.GetBytes(csv.ToString());
                return File(content, "text/csv", $"{semester.Name.Replace(" ", "_")}_Courses_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting courses to CSV");
                TempData["ErrorMessage"] = "Error generating CSV file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportSemesterSummaryToPdf(Semester semester, List<Course> courses, List<CourseEnrollment> enrollments)
        {
            try
            {
                var summary = new StringBuilder();
                summary.AppendLine($"SEMESTER SUMMARY REPORT");
                summary.AppendLine($"=======================");
                summary.AppendLine();
                summary.AppendLine($"Semester: {semester.Name}");
                summary.AppendLine($"Academic Year: {semester.AcademicYear}");
                summary.AppendLine($"Type: {semester.SemesterType}");
                summary.AppendLine($"Duration: {semester.StartDate:MMM dd, yyyy} to {semester.EndDate:MMM dd, yyyy}");
                summary.AppendLine($"Registration: {semester.RegistrationStartDate:MMM dd, yyyy} to {semester.RegistrationEndDate:MMM dd, yyyy}");
                summary.AppendLine($"Status: {(semester.IsActive ? "Active" : "Inactive")}");
                summary.AppendLine($"Current Semester: {(semester.IsCurrent ? "Yes" : "No")}");
                summary.AppendLine($"Registration Open: {(semester.IsRegistrationOpen ? "Yes" : "No")}");
                summary.AppendLine();

                // Statistics
                var totalEnrollments = enrollments.Count;
                var totalCapacity = courses.Sum(c => c.MaxStudents);
                var utilizationRate = totalCapacity > 0 ? (totalEnrollments * 100.0) / totalCapacity : 0;
                var fullCourses = courses.Count(c => (c.CourseEnrollments?.Count(ce => ce.IsActive) ?? 0) >= c.MaxStudents);
                var emptyCourses = courses.Count(c => (c.CourseEnrollments?.Count(ce => ce.IsActive) ?? 0) == 0);

                summary.AppendLine($"STATISTICS");
                summary.AppendLine($"==========");
                summary.AppendLine($"Total Courses: {courses.Count}");
                summary.AppendLine($"Total Enrollments: {totalEnrollments}");
                summary.AppendLine($"Total Capacity: {totalCapacity}");
                summary.AppendLine($"Utilization Rate: {utilizationRate:0.00}%");
                summary.AppendLine($"Full Courses: {fullCourses}");
                summary.AppendLine($"Empty Courses: {emptyCourses}");
                summary.AppendLine();

                // Course list
                summary.AppendLine($"COURSES");
                summary.AppendLine($"=======");
                foreach (var course in courses.OrderBy(c => c.CourseCode))
                {
                    var currentEnrollment = course.CourseEnrollments?.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;
                    summary.AppendLine($"{course.CourseCode} - {course.CourseName}");
                    summary.AppendLine($"  Credits: {course.Credits}, Enrollment: {currentEnrollment}/{course.MaxStudents}, Status: {(course.IsActive ? "Active" : "Inactive")}");
                    summary.AppendLine();
                }

                var content = Encoding.UTF8.GetBytes(summary.ToString());
                return File(content, "text/plain", $"{semester.Name.Replace(" ", "_")}_Summary_{DateTime.Now:yyyyMMdd}.txt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting semester summary");
                TempData["ErrorMessage"] = "Error generating summary file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportSemesterDetailsToExcel(Semester semester, List<Course> courses, List<CourseEnrollment> enrollments)
        {
            try
            {
                using var package = new ExcelPackage();

                // Semester Info Sheet
                var infoSheet = package.Workbook.Worksheets.Add("Semester Information");
                infoSheet.Cells[1, 1].Value = "Semester Details";
                infoSheet.Cells[1, 1].Style.Font.Bold = true;

                var infoData = new[]
                {
            new[] { "Name:", semester.Name },
            new[] { "Academic Year:", semester.AcademicYear.ToString() },
            new[] { "Type:", semester.SemesterType },
            new[] { "Start Date:", semester.StartDate.ToString("MMM dd, yyyy") },
            new[] { "End Date:", semester.EndDate.ToString("MMM dd, yyyy") },
            new[] { "Registration Start:", semester.RegistrationStartDate.ToString("MMM dd, yyyy") },
            new[] { "Registration End:", semester.RegistrationEndDate.ToString("MMM dd, yyyy") },
            new[] { "Active:", semester.IsActive ? "Yes" : "No" },
            new[] { "Current Semester:", semester.IsCurrent ? "Yes" : "No" },
            new[] { "Registration Open:", semester.IsRegistrationOpen ? "Yes" : "No" }
        };

                for (int i = 0; i < infoData.Length; i++)
                {
                    infoSheet.Cells[i + 2, 1].Value = infoData[i][0];
                    infoSheet.Cells[i + 2, 2].Value = infoData[i][1];
                }

                // Courses Sheet
                var coursesSheet = package.Workbook.Worksheets.Add("Courses");
                var courseHeaders = new[] { "Course Code", "Course Name", "Credits", "Department", "Max Students", "Current Enrollment", "Available Seats", "Utilization %", "Status" };
                for (int i = 0; i < courseHeaders.Length; i++)
                {
                    coursesSheet.Cells[1, i + 1].Value = courseHeaders[i];
                    coursesSheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int courseRow = 2;
                foreach (var course in courses)
                {
                    var currentEnrollment = course.CourseEnrollments?.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;
                    var availableSeats = course.MaxStudents - currentEnrollment;
                    var utilization = course.MaxStudents > 0 ? (currentEnrollment * 100.0) / course.MaxStudents : 0;

                    coursesSheet.Cells[courseRow, 1].Value = course.CourseCode;
                    coursesSheet.Cells[courseRow, 2].Value = course.CourseName;
                    coursesSheet.Cells[courseRow, 3].Value = course.Credits;
                    coursesSheet.Cells[courseRow, 4].Value = course.Department ?? "N/A";
                    coursesSheet.Cells[courseRow, 5].Value = course.MaxStudents;
                    coursesSheet.Cells[courseRow, 6].Value = currentEnrollment;
                    coursesSheet.Cells[courseRow, 7].Value = availableSeats;
                    coursesSheet.Cells[courseRow, 8].Value = Math.Round(utilization, 2);
                    coursesSheet.Cells[courseRow, 9].Value = course.IsActive ? "Active" : "Inactive";
                    courseRow++;
                }

                // Enrollments Sheet
                var enrollmentsSheet = package.Workbook.Worksheets.Add("Enrollments");
                var enrollmentHeaders = new[] { "Student ID", "Student Name", "Course Code", "Course Name", "Enrollment Date", "Status" };
                for (int i = 0; i < enrollmentHeaders.Length; i++)
                {
                    enrollmentsSheet.Cells[1, i + 1].Value = enrollmentHeaders[i];
                    enrollmentsSheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int enrollmentRow = 2;
                foreach (var enrollment in enrollments.OrderBy(e => e.Course?.CourseCode).ThenBy(e => e.Student?.Name))
                {
                    enrollmentsSheet.Cells[enrollmentRow, 1].Value = enrollment.Student?.StudentId ?? "N/A";
                    enrollmentsSheet.Cells[enrollmentRow, 2].Value = enrollment.Student?.Name ?? "Unknown";
                    enrollmentsSheet.Cells[enrollmentRow, 3].Value = enrollment.Course?.CourseCode ?? "N/A";
                    enrollmentsSheet.Cells[enrollmentRow, 4].Value = enrollment.Course?.CourseName ?? "Unknown";
                    enrollmentsSheet.Cells[enrollmentRow, 5].Value = enrollment.CreatedDate.ToString("MMM dd, yyyy");
                    enrollmentsSheet.Cells[enrollmentRow, 6].Value = enrollment.EnrollmentStatus.ToString();
                    enrollmentRow++;
                }

                // Auto-fit all columns
                infoSheet.Cells[infoSheet.Dimension.Address].AutoFitColumns();
                coursesSheet.Cells[coursesSheet.Dimension.Address].AutoFitColumns();
                enrollmentsSheet.Cells[enrollmentsSheet.Dimension.Address].AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{semester.Name.Replace(" ", "_")}_Detailed_Report_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportSemesterDetailsToExcel");
                TempData["ErrorMessage"] = "Error generating Excel file.";
                return RedirectToAction(nameof(Details), new { id = semester.Id });
            }
        }

        // Helper method for enhanced filtering in the Details view
        [HttpGet]
        public async Task<IActionResult> GetFilteredCourses(int semesterId,
            string searchTerm = "",
            string department = "",
            string status = "",
            string enrollmentStatus = "",
            string credits = "")
        {
            try
            {
                var query = _context.Courses
                    .Include(c => c.CourseDepartment)
                    .Include(c => c.CourseEnrollments)
                    .Where(c => c.SemesterId == semesterId && c.IsActive);

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c =>
                        c.CourseCode.Contains(searchTerm) ||
                        c.CourseName.Contains(searchTerm) ||
                        (c.Description != null && c.Description.Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(department))
                {
                    query = query.Where(c => c.Department == department);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    bool isActive = status == "active";
                    query = query.Where(c => c.IsActive == isActive);
                }

                if (!string.IsNullOrEmpty(enrollmentStatus))
                {
                    query = enrollmentStatus switch
                    {
                        "full" => query.Where(c => c.CourseEnrollments.Count(ce => ce.IsActive) >= c.MaxStudents),
                        "empty" => query.Where(c => c.CourseEnrollments.Count(ce => ce.IsActive) == 0),
                        "available" => query.Where(c => c.CourseEnrollments.Count(ce => ce.IsActive) > 0 &&
                                                      c.CourseEnrollments.Count(ce => ce.IsActive) < c.MaxStudents),
                        _ => query
                    };
                }

                if (!string.IsNullOrEmpty(credits) && decimal.TryParse(credits, out decimal creditValue))
                {
                    query = query.Where(c => c.Credits == creditValue);
                }

                var courses = await query
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department ?? "No Department",
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        isActive = c.IsActive,
                        description = c.Description,
                        availableSeats = c.MaxStudents - c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        utilization = c.MaxStudents > 0 ?
                            (c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active) * 100.0) / c.MaxStudents : 0
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }
        //////////
        ///
        [HttpPost]
        public async Task<IActionResult> ExportSemesters(string semesterIds, string exportType)
        {
            try
            {
                var ids = string.IsNullOrEmpty(semesterIds)
                    ? new List<int>()
                    : semesterIds.Split(',').Select(int.Parse).ToList();

                IQueryable<Semester> query = _context.Semesters
                    .Include(s => s.Department)
                    .Include(s => s.Branch)
                    .Include(s => s.SubBranch);

                if (ids.Any())
                {
                    query = query.Where(s => ids.Contains(s.Id));
                }

                var semesters = await query
                    .OrderByDescending(s => s.AcademicYear)
                    .ThenBy(s => s.StartDate)
                    .ToListAsync();

                // All these methods are now synchronous
                if (exportType?.ToLower() == "excel")
                {
                    return ExportSemestersToExcel(semesters); // No await
                }
                else if (exportType?.ToLower() == "csv")
                {
                    return ExportSemestersToCsv(semesters); // No await
                }

                TempData["ErrorMessage"] = $"Invalid export type: {exportType}. Use 'excel' or 'csv'.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting semesters");
                TempData["ErrorMessage"] = "Error exporting semesters data.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportSemesterOverview(string exportType)
        {
            try
            {
                var semesters = await _context.Semesters
                    .Include(s => s.Department)
                    .Include(s => s.Branch)
                    .Include(s => s.SubBranch)
                    .OrderByDescending(s => s.AcademicYear)
                    .ThenBy(s => s.StartDate)
                    .ToListAsync();

                // FIX: Get courses count for each semester
                foreach (var semester in semesters)
                {
                    // Add course count as a dynamic property or use ViewBag
                    ViewData[$"CourseCount_{semester.Id}"] = await _context.Courses
                        .CountAsync(c => c.SemesterId == semester.Id);
                }

                // FIX THESE LINES - REMOVE 'await' since these methods are synchronous
                if (exportType?.ToLower() == "summary")
                {
                    return ExportOverviewToPdf(semesters); // Line ~1819 - NO AWAIT
                }
                else if (exportType?.ToLower() == "details")
                {
                    return ExportOverviewToExcel(semesters); // Line ~1823 - NO AWAIT
                }

                TempData["ErrorMessage"] = $"Invalid export type: {exportType}. Use 'summary' or 'details'.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting semester overview");
                TempData["ErrorMessage"] = "Error exporting semester overview.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportSemestersToExcel(List<Semester> semesters)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Semesters");

                // Header
                worksheet.Cells[1, 1].Value = "Semesters Export";
                worksheet.Cells[1, 1, 1, 8].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;

                // Column headers
                var headers = new[] { "Name", "Type", "Academic Year", "Department", "Start Date", "End Date", "Status", "Current" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[3, i + 1].Value = headers[i];
                    worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                }

                // Data rows
                int row = 4;
                foreach (var semester in semesters)
                {
                    worksheet.Cells[row, 1].Value = semester.Name;
                    worksheet.Cells[row, 2].Value = semester.SemesterType;
                    worksheet.Cells[row, 3].Value = semester.AcademicYear;
                    worksheet.Cells[row, 4].Value = semester.Department?.Name ?? "N/A";
                    worksheet.Cells[row, 5].Value = semester.StartDate.ToString("MMM dd, yyyy");
                    worksheet.Cells[row, 6].Value = semester.EndDate.ToString("MMM dd, yyyy");
                    worksheet.Cells[row, 7].Value = semester.IsActive ? "Active" : "Inactive";
                    worksheet.Cells[row, 8].Value = semester.IsCurrent ? "Yes" : "No";
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Semesters_Export_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportSemestersToExcel");
                TempData["ErrorMessage"] = "Error generating Excel file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportSemestersToCsv(List<Semester> semesters)
        {
            try
            {
                var csv = new StringBuilder();

                // CSV headers
                csv.AppendLine("Name,Semester Type,Academic Year,Department,Start Date,End Date,Registration Start,Registration End,Active,Current,Registration Open");

                // Data rows
                foreach (var semester in semesters)
                {
                    var line = $"\"{semester.Name}\",\"{semester.SemesterType}\",{semester.AcademicYear},\"{semester.Department?.Name ?? "N/A"}\",\"{semester.StartDate:yyyy-MM-dd}\",\"{semester.EndDate:yyyy-MM-dd}\",\"{semester.RegistrationStartDate:yyyy-MM-dd}\",\"{semester.RegistrationEndDate:yyyy-MM-dd}\",\"{(semester.IsActive ? "Yes" : "No")}\",\"{(semester.IsCurrent ? "Yes" : "No")}\",\"{(semester.IsRegistrationOpen ? "Yes" : "No")}\"";
                    csv.AppendLine(line);
                }

                var content = Encoding.UTF8.GetBytes(csv.ToString());
                return File(content, "text/csv", $"Semesters_Export_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportSemestersToCsv");
                TempData["ErrorMessage"] = "Error generating CSV file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportOverviewToPdf(List<Semester> semesters)
        {
            try
            {
                // Generate PDF using QuestPDF with correct namespaces
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4); // FIXED: PageSizes is in Helpers
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .AlignCenter()
                            .Text("SEMESTER MANAGEMENT OVERVIEW REPORT")
                            .SemiBold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);

                        page.Content()
                            .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(10);

                                // Generation info
                                column.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
                                column.Item().Text($"Total Semesters: {semesters.Count}");

                                // Statistics section
                                column.Item().PaddingTop(10).Text("KEY STATISTICS").SemiBold().FontSize(14);

                                var activeSemesters = semesters.Count(s => s.IsActive);
                                var currentSemester = semesters.FirstOrDefault(s => s.IsCurrent);
                                var registrationOpen = semesters.Count(s => s.IsRegistrationOpen);
                                var upcomingSemesters = semesters.Count(s => s.StartDate > DateTime.Now);

                                column.Item().Text($"Active Semesters: {activeSemesters}");
                                column.Item().Text($"Registration Open: {registrationOpen}");
                                column.Item().Text($"Upcoming Semesters: {upcomingSemesters}");
                                column.Item().Text($"Current Semester: {currentSemester?.Name ?? "None"}");

                                // Semester type distribution
                                column.Item().PaddingTop(10).Text("SEMESTER TYPE DISTRIBUTION").SemiBold().FontSize(14);
                                column.Item().Text($"Fall: {semesters.Count(s => s.SemesterType == "Fall")}");
                                column.Item().Text($"Spring: {semesters.Count(s => s.SemesterType == "Spring")}");
                                column.Item().Text($"Summer: {semesters.Count(s => s.SemesterType == "Summer")}");

                                // Academic year range
                                if (semesters.Any())
                                {
                                    column.Item().PaddingTop(10).Text($"Academic Year Range: {semesters.Min(s => s.AcademicYear)} - {semesters.Max(s => s.AcademicYear)}");
                                }

                                // Semester list
                                column.Item().PaddingTop(10).Text("SEMESTER LIST").SemiBold().FontSize(14);

                                foreach (var semester in semesters.OrderByDescending(s => s.AcademicYear).ThenBy(s => s.StartDate))
                                {
                                    column.Item().PaddingTop(5).Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Column(semCol =>
                                    {
                                        semCol.Item().Text($"{semester.Name} ({semester.SemesterType} {semester.AcademicYear})").SemiBold();
                                        semCol.Item().Text($"Department: {semester.Department?.Name ?? "N/A"}");
                                        semCol.Item().Text($"Duration: {semester.StartDate:MMM dd, yyyy} to {semester.EndDate:MMM dd, yyyy}");

                                        var status = new List<string>();
                                        if (semester.IsActive) status.Add("Active");
                                        if (semester.IsCurrent) status.Add("CURRENT");
                                        if (semester.IsRegistrationOpen) status.Add("REG OPEN");

                                        semCol.Item().Text($"Status: {string.Join(" | ", status)}");
                                    });
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf", $"Semester_Overview_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportOverviewToPdf");
                TempData["ErrorMessage"] = "Error generating PDF file.";
                return RedirectToAction(nameof(Index));
            }
        }

        private IActionResult ExportOverviewToExcel(List<Semester> semesters)
        {
            try
            {
                using var package = new ExcelPackage();

                // Summary sheet
                var summarySheet = package.Workbook.Worksheets.Add("Summary");
                summarySheet.Cells[1, 1].Value = "Semester Management Overview";
                summarySheet.Cells[1, 1, 1, 5].Merge = true;
                summarySheet.Cells[1, 1].Style.Font.Bold = true;

                // Add summary statistics
                var stats = new[]
                {
            new[] { "Total Semesters:", semesters.Count.ToString() },
            new[] { "Active Semesters:", semesters.Count(s => s.IsActive).ToString() },
            new[] { "Current Semester:", semesters.FirstOrDefault(s => s.IsCurrent)?.Name ?? "None" },
            new[] { "Registration Open:", semesters.Count(s => s.IsRegistrationOpen).ToString() },
            new[] { "Upcoming Semesters:", semesters.Count(s => s.StartDate > DateTime.Now).ToString() }
        };

                for (int i = 0; i < stats.Length; i++)
                {
                    summarySheet.Cells[i + 3, 1].Value = stats[i][0];
                    summarySheet.Cells[i + 3, 2].Value = stats[i][1];
                }

                // Detailed sheet
                var detailsSheet = package.Workbook.Worksheets.Add("Semester Details");
                var headers = new[] { "Name", "Type", "Academic Year", "Department", "Start Date", "End Date", "Registration Start", "Registration End", "Active", "Current", "Registration Open" };

                for (int i = 0; i < headers.Length; i++)
                {
                    detailsSheet.Cells[1, i + 1].Value = headers[i];
                    detailsSheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var semester in semesters.OrderByDescending(s => s.AcademicYear).ThenBy(s => s.StartDate))
                {
                    detailsSheet.Cells[row, 1].Value = semester.Name;
                    detailsSheet.Cells[row, 2].Value = semester.SemesterType;
                    detailsSheet.Cells[row, 3].Value = semester.AcademicYear;
                    detailsSheet.Cells[row, 4].Value = semester.Department?.Name ?? "N/A";
                    detailsSheet.Cells[row, 5].Value = semester.StartDate;
                    detailsSheet.Cells[row, 6].Value = semester.EndDate;
                    detailsSheet.Cells[row, 7].Value = semester.RegistrationStartDate;
                    detailsSheet.Cells[row, 8].Value = semester.RegistrationEndDate;
                    detailsSheet.Cells[row, 9].Value = semester.IsActive ? "Yes" : "No";
                    detailsSheet.Cells[row, 10].Value = semester.IsCurrent ? "Yes" : "No";
                    detailsSheet.Cells[row, 11].Value = semester.IsRegistrationOpen ? "Yes" : "No";
                    row++;
                }

                // Auto-fit columns
                detailsSheet.Cells[detailsSheet.Dimension.Address].AutoFitColumns();
                summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Semester_Overview_Detailed_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportOverviewToExcel");
                TempData["ErrorMessage"] = "Error generating Excel file.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdate(List<int> semesterIds, string action)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var semesters = await _context.Semesters
                    .Where(s => semesterIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var semester in semesters)
                {
                    switch (action.ToLower())
                    {
                        case "activate":
                            semester.IsActive = true;
                            break;
                        case "deactivate":
                            semester.IsActive = false;
                            semester.IsCurrent = false;
                            semester.IsRegistrationOpen = false;
                            break;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"{semesters.Count} semesters updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update for semesters");
                return Json(new { success = false, message = "Error updating semesters." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkDelete(List<int> semesterIds)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var semesters = await _context.Semesters
                    .Where(s => semesterIds.Contains(s.Id))
                    .ToListAsync();

                // FIX: Check which semesters have courses using direct query
                var semesterIdsWithCourses = await _context.Courses
                    .Where(c => semesterIds.Contains(c.SemesterId ?? 0))
                    .Select(c => c.SemesterId ?? 0)
                    .Distinct()
                    .ToListAsync();

                var deletableSemesters = semesters.Where(s => !semesterIdsWithCourses.Contains(s.Id)).ToList();
                var nonDeletableSemesters = semesters.Where(s => semesterIdsWithCourses.Contains(s.Id)).ToList();

                if (nonDeletableSemesters.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"{nonDeletableSemesters.Count} semester(s) cannot be deleted because they have associated courses."
                    });
                }

                _context.Semesters.RemoveRange(deletableSemesters);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{deletableSemesters.Count} semester(s) deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk delete for semesters");
                return Json(new { success = false, message = "Error deleting semesters." });
            }
        }

    }

}
/// <summary>
/// ////////////////
/// </summary>
public class RemoveCoursesRequest
{
    public int SemesterId { get; set; }
    public List<int> CourseIds { get; set; } = new List<int>();

}

public class AddCoursesRequest
{
    public int SemesterId { get; set; }
    public List<int> CourseIds { get; set; } = new List<int>();
}

public class BulkEnrollRequest
{
    public int SemesterId { get; set; }
    public List<int> CourseIds { get; set; } = new List<int>();
    public List<int> StudentIds { get; set; } = new List<int>();
}

//public class SemesterDetailsViewModel
//{
//    public Semester Semester { get; set; } = null!;
//    public List<CourseWithEnrollment> Courses { get; set; } = new List<CourseWithEnrollment>();
//}

//public class CourseWithEnrollment
//{
//    public Course Course { get; set; } = null!;
//    public int CurrentEnrollment { get; set; }
//}

