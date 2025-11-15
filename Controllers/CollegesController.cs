using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels; // ADD THIS LINE
using System.Linq;
using System.Threading.Tasks;


namespace StudentManagementSystem.Controllers
{
    public class CollegesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CollegesController> _logger;

        public CollegesController(ApplicationDbContext context, ILogger<CollegesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Colleges
        public async Task<IActionResult> Index()
        {
            var colleges = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Departments)
                .Where(c => c.IsActive)
                .OrderBy(c => c.University != null ? c.University.Name : "")
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(colleges);
        }

        //// GET: Colleges/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var college = await _context.Colleges
        //        .Include(c => c.University)
        //        .Include(c => c.Departments)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (college == null) return NotFound();

        //    return View(college);
        //}

        // GET: Colleges/Create
        //public async Task<IActionResult> Create()
        //{
        //    await PopulateUniversitiesDropdown();
        //    return View();
        //}

        // POST: Colleges/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CollegeCode,Description,UniversityId,IsActive")] College college)
        {
            if (ModelState.IsValid)
            {
                // Check if college name already exists in the same university
                var existingCollege = await _context.Colleges
                    .AnyAsync(c => c.UniversityId == college.UniversityId &&
                                  c.Name == college.Name &&
                                  c.IsActive);

                if (existingCollege)
                {
                    ModelState.AddModelError("Name", "A college with this name already exists in the selected university.");
                    await PopulateUniversitiesDropdown();
                    return View(college);
                }

                _context.Add(college);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"College '{college.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUniversitiesDropdown();
            return View(college);
        }

        // GET: Colleges/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var college = await _context.Colleges.FindAsync(id);
            if (college == null) return NotFound();

            await PopulateUniversitiesDropdown();
            return View(college);
        }

        // POST: Colleges/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CollegeCode,Description,UniversityId,IsActive")] College college)
        {
            if (id != college.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if college name already exists in the same university (excluding current)
                    var existingCollege = await _context.Colleges
                        .AnyAsync(c => c.UniversityId == college.UniversityId &&
                                      c.Name == college.Name &&
                                      c.Id != id &&
                                      c.IsActive);

                    if (existingCollege)
                    {
                        ModelState.AddModelError("Name", "A college with this name already exists in the selected university.");
                        await PopulateUniversitiesDropdown();
                        return View(college);
                    }

                    _context.Update(college);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"College '{college.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CollegeExists(college.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateUniversitiesDropdown();
            return View(college);
        }

        //// GET: Colleges/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var college = await _context.Colleges
        //        .Include(c => c.University)
        //        .Include(c => c.Departments)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (college == null) return NotFound();

        //    if (college.Departments.Any())
        //    {
        //        TempData["ErrorMessage"] = "Cannot delete college because it has associated departments. Delete the departments first.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(college);
        //}

        //// POST: Colleges/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var college = await _context.Colleges
        //        .Include(c => c.Departments)
        //        .FirstOrDefaultAsync(c => c.Id == id);

        //    if (college == null) return NotFound();

        //    if (college.Departments.Any())
        //    {
        //        TempData["ErrorMessage"] = "Cannot delete college because it has associated departments. Delete the departments first.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    _context.Colleges.Remove(college);
        //    await _context.SaveChangesAsync();

        //    TempData["SuccessMessage"] = $"College '{college.Name}' deleted successfully.";
        //    return RedirectToAction(nameof(Index));
        //}
        // GET: Colleges/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Departments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            if (college.Departments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete college because it has associated departments. Delete the departments first.";
                return RedirectToAction(nameof(Index));
            }

            return View(college);
        }

        // POST: Colleges/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var college = await _context.Colleges
                .Include(c => c.Departments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            if (college.Departments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete college because it has associated departments. Delete the departments first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Colleges.Remove(college);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"College '{college.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        // GET: Colleges/Duplicate/5
        public async Task<IActionResult> Duplicate(int? id)
        {
            if (id == null) return NotFound();

            var college = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Departments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (college == null) return NotFound();

            var viewModel = new StudentManagementSystem.Models.ViewModels.DuplicateViewModel
            {
                EntityType = "College",
                SourceId = college.Id,
                NewName = $"{college.Name} - Copy",
                TargetParentId = college.UniversityId
            };

            await PopulateUniversitiesDropdown();
            return View(viewModel);
        }

        // POST: Colleges/Duplicate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id, DuplicateViewModel model)
        {
            if (id != model.SourceId) return NotFound();

            var sourceCollege = await _context.Colleges
                .Include(c => c.Departments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (sourceCollege == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Create new college
                    var newCollege = new College
                    {
                        Name = model.NewName,
                        CollegeCode = $"{sourceCollege.CollegeCode}-COPY",
                        Description = sourceCollege.Description,
                        UniversityId = model.TargetParentId ?? sourceCollege.UniversityId,
                        IsActive = sourceCollege.IsActive
                    };

                    _context.Colleges.Add(newCollege);
                    await _context.SaveChangesAsync();

                    // Duplicate departments if requested
                    if (model.CopySubItems && sourceCollege.Departments.Any())
                    {
                        foreach (var department in sourceCollege.Departments.Where(d => d.IsActive))
                        {
                            var newDepartment = new Department
                            {
                                Name = department.Name,
                                DepartmentCode = $"{department.DepartmentCode}-COPY",
                                Description = department.Description,
                                CollegeId = newCollege.Id,
                                StartYear = department.StartYear,
                                IsMajorDepartment = department.IsMajorDepartment,
                                MinimumGPAMajor = department.MinimumGPAMajor,
                                TotalBenches = department.TotalBenches,
                                AvailableBenches = department.AvailableBenches,
                                IsActive = department.IsActive
                            };
                            _context.Departments.Add(newDepartment);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"College '{sourceCollege.Name}' duplicated successfully as '{model.NewName}'.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error duplicating college {CollegeId}", id);
                    ModelState.AddModelError("", "An error occurred while duplicating the college.");
                }
            }

            await PopulateUniversitiesDropdown();
            return View(model);
        }

        private bool CollegeExists(int id)
        {
            return _context.Colleges.Any(e => e.Id == id);
        }

        private async Task PopulateUniversitiesDropdown()
        {
            var universities = await _context.Universities
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            ViewBag.UniversityId = new SelectList(universities, "Id", "Name");
        }

        public async Task<IActionResult> Create(int? universityId)
        {
            if (universityId.HasValue)
            {
                var university = await _context.Universities.FindAsync(universityId);
                ViewBag.ParentUniversity = university;
            }

            // Load universities for dropdown
            ViewBag.Universities = await _context.Universities
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToListAsync();

            return View();
        }

        private async Task LoadUniversitiesViewBag()
        {
            var universities = await _context.Universities
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            ViewBag.UniversityId = new SelectList(universities, "Id", "Name");
        }

        // GET: Colleges/Details/5
        // GET: Colleges/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var college = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Departments)
                    .ThenInclude(d => d.Branches)
                .Include(c => c.Departments)
                    .ThenInclude(d => d.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (college == null) return NotFound();

            return View(college);
        }


    }
}