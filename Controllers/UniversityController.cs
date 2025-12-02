// Controllers/UniversityController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Threading.Tasks;
using StudentManagementSystem.Models.ViewModels;
//using DuplicateViewModel = StudentManagementSystem.Models.ViewModels.DuplicateViewModel;




namespace StudentManagementSystem.Controllers
{
    public class UniversityController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(ApplicationDbContext context, ILogger<UniversityController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: University
        public async Task<IActionResult> Index()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var universities = await _context.Universities
                .Include(u => u.Colleges)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return View(universities);
        }

        // GET: University/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (university == null) return NotFound();

            return View(university);
        }

        // GET: University/Create
        public IActionResult Create()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            return View();
        }

        // POST: University/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Code,Description,Address,Email,Phone,Website,EstablishmentYear,IsActive,AllowMultipleColleges")] University university)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                // Check if code already exists
                var existingCode = await _context.Universities
                    .AnyAsync(u => u.Code == university.Code);

                if (existingCode)
                {
                    ModelState.AddModelError("Code", "University code already exists.");
                    return View(university);
                }

                _context.Add(university);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"University '{university.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(university);
        }

        // GET: University/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id == null) return NotFound();

            var university = await _context.Universities.FindAsync(id);
            if (university == null) return NotFound();

            return View(university);
        }

        // POST: University/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,Address,Email,Phone,Website,EstablishmentYear,IsActive,AllowMultipleColleges")] University university)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id != university.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if code already exists (excluding current university)
                    var existingCode = await _context.Universities
                        .AnyAsync(u => u.Code == university.Code && u.Id != id);

                    if (existingCode)
                    {
                        ModelState.AddModelError("Code", "University code already exists.");
                        return View(university);
                    }

                    _context.Update(university);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"University '{university.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UniversityExists(university.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(university);
        }

        // GET: University/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (university == null) return NotFound();

            if (university.Colleges.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete university because it has associated colleges. Delete the colleges first.";
                return RedirectToAction(nameof(Index));
            }

            return View(university);
        }

        // POST: University/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (university == null)
            {
                return NotFound();
            }

            if (university.Colleges.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete university because it has associated colleges. Delete the colleges first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Universities.Remove(university);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"University '{university.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: University/Duplicate/5
        public async Task<IActionResult> Duplicate(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id == null) return NotFound();

            var university = await _context.Universities.FindAsync(id);
            if (university == null) return NotFound();

            var viewModel = new DuplicateViewModel
            {
                EntityType = "University",
                SourceId = university.Id,
                NewName = $"{university.Name} - Copy"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id, DuplicateViewModel model)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id != model.SourceId) return NotFound();

            var sourceUniversity = await _context.Universities
                .Include(u => u.Colleges)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (sourceUniversity == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Create new university
                    var newUniversity = new University
                    {
                        Name = model.NewName,
                        Code = $"{sourceUniversity.Code}-COPY",
                        Description = sourceUniversity.Description,
                        Address = sourceUniversity.Address,
                        Email = sourceUniversity.Email,
                        Phone = sourceUniversity.Phone,
                        Website = sourceUniversity.Website,
                        EstablishmentYear = sourceUniversity.EstablishmentYear,
                        IsActive = sourceUniversity.IsActive,
                        AllowMultipleColleges = sourceUniversity.AllowMultipleColleges
                    };

                    _context.Universities.Add(newUniversity);
                    await _context.SaveChangesAsync();

                    // Duplicate colleges if requested
                    if (model.CopySubItems && sourceUniversity.Colleges.Any())
                    {
                        foreach (var college in sourceUniversity.Colleges.Where(c => c.IsActive))
                        {
                            var newCollege = new College
                            {
                                Name = college.Name,
                                CollegeCode = $"{college.CollegeCode}-COPY",
                                Description = college.Description,
                                UniversityId = newUniversity.Id,
                                IsActive = college.IsActive
                            };
                            _context.Colleges.Add(newCollege);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"University '{sourceUniversity.Name}' duplicated successfully as '{model.NewName}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error duplicating university {UniversityId}", id);
                    ModelState.AddModelError("", "An error occurred while duplicating the university.");
                }
            }

            return View(model);
        }

        // Update the Dashboard method:
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var universities = await _context.Universities
                .Include(u => u.Colleges)
                .Where(u => u.IsActive)
                .ToListAsync();

            var totalColleges = universities.Sum(u => u.Colleges.Count);
            var totalStudents = await _context.Students.CountAsync();
            var totalCourses = await _context.Courses.CountAsync(c => c.IsActive);
            var activeSemesters = await _context.Semesters.CountAsync(s => s.IsActive);

            var dashboardModel = new UniversityDashboardViewModel
            {
                Universities = universities,
                TotalUniversities = universities.Count,
                TotalColleges = totalColleges,
                TotalStudents = totalStudents,
                TotalCourses = totalCourses,
                ActiveSemesters = activeSemesters
            };

            return View(dashboardModel);
        }

        // GET: University/Dashboard/5
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (id == null) return NotFound();

            var university = await _context.Universities
                .Include(u => u.Colleges)
                    .ThenInclude(c => c.Departments)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (university == null) return NotFound();

            var totalColleges = university.Colleges.Count;
            var totalDepartments = university.Colleges.Sum(c => c.Departments.Count);

            // Get college IDs for this university
            var collegeIds = university.Colleges.Select(c => c.Id).ToList();

            // Get department IDs for these colleges
            var departmentIds = await _context.Departments
                .Where(d => collegeIds.Contains(d.CollegeId ?? 0))
                .Select(d => d.Id)
                .ToListAsync();

            // Count students using the CORRECT navigation property name
            var totalStudents = await _context.Students
                .Include(s => s.StudentDepartment) // ← Use the new name
                .Where(s => s.DepartmentId != null && departmentIds.Contains(s.DepartmentId.Value))
                .CountAsync();

            // Count courses using the CORRECT navigation property name  
            var totalCourses = await _context.Courses
                .Include(c => c.CourseDepartment) // ← Use the new name
                .Where(c => c.DepartmentId != null && departmentIds.Contains(c.DepartmentId.Value))
                .CountAsync();

            var viewModel = new UniversityDashboardViewModel
            {
                University = university,
                TotalColleges = totalColleges,
                TotalDepartments = totalDepartments,
                TotalStudents = totalStudents,
                TotalCourses = totalCourses
            };

            return View("UniversityDashboard", viewModel);
        }

        // GET: University/StructureOverview
        public async Task<IActionResult> StructureOverview()
        {
            var universities = await _context.Universities
                .Include(u => u.Colleges!)
                    .ThenInclude(c => c.Departments!)
                .Where(u => u.IsActive)
                .ToListAsync();

            return View(universities);
        }

        private bool UniversityExists(int id)
        {
            return _context.Universities.Any(e => e.Id == id);
        }
        // GET: University/Colleges/5
        public async Task<IActionResult> Colleges(int? id)
        {
            if (id == null) return NotFound();

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (university == null) return NotFound();

            return View(university);
        }

    }
}