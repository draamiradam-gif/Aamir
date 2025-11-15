// Controllers/BranchesController.cs
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
using StudentManagementSystem.ViewModels;


namespace StudentManagementSystem.Controllers
{
    public class BranchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(ApplicationDbContext context, ILogger<BranchesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Branches
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .Include(b => b.Department!)
                    .ThenInclude(d => d.College!)
                        .ThenInclude(c => c.University)
                .Include(b => b.SubBranches)
                .Include(b => b.BranchSemesters)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Department!.College!.University!.Name)
                .ThenBy(b => b.Department!.College!.Name)
                .ThenBy(b => b.Department!.Name)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return View(branches);
        }

        // GET: Branches/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var branch = await _context.Branches
        //        .Include(b => b.Department!)
        //            .ThenInclude(d => d.College!)
        //                .ThenInclude(c => c.University)
        //        .Include(b => b.ParentBranch)
        //        .Include(b => b.SubBranches)
        //        .Include(b => b.BranchSemesters)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (branch == null) return NotFound();

        //    return View(branch);
        //}

        // GET: Branches/Create
        //public async Task<IActionResult> Create()
        //{
        //    await PopulateDepartmentsDropdown();
        //    await PopulateParentBranchesDropdown();
        //    return View();
        //}

        

        // GET: Branches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            await PopulateDepartmentsDropdown();
            await PopulateParentBranchesDropdown();
            return View(branch);
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,BranchCode,Description,DepartmentId,ParentBranchId,IsActive")] Branch branch)
        {
            if (id != branch.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Branch '{branch.Name}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDepartmentsDropdown();
            await PopulateParentBranchesDropdown();
            return View(branch);
        }

        // GET: Branches/ManageSubBranches/5
        public async Task<IActionResult> ManageSubBranches(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches
                .Include(b => b.Department)
                .Include(b => b.SubBranches)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return NotFound();

            var viewModel = new BranchSubBranchesViewModel
            {
                ParentBranch = branch,
                NewSubBranch = new Branch
                {
                    DepartmentId = branch.DepartmentId,
                    ParentBranchId = branch.Id
                }
            };

            return View(viewModel);
        }

        //// POST: Branches/AddSubBranch
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddSubBranch(Branch newSubBranch)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Check if branch name already exists in the same department
        //        var existingBranch = await _context.Branches
        //            .AnyAsync(b => b.DepartmentId == newSubBranch.DepartmentId &&
        //                          b.Name == newSubBranch.Name &&
        //                          b.IsActive);

        //        if (existingBranch)
        //        {
        //            TempData["ErrorMessage"] = "A branch with this name already exists in this department.";
        //            return RedirectToAction("ManageSubBranches", new { id = newSubBranch.ParentBranchId });
        //        }

        //        _context.Add(newSubBranch);
        //        await _context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = $"Sub-branch '{newSubBranch.Name}' added successfully.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "Invalid sub-branch data.";
        //    }

        //    return RedirectToAction("ManageSubBranches", new { id = newSubBranch.ParentBranchId });
        //}

        // GET: Branches/ByDepartment/5
        public async Task<JsonResult> ByDepartment(int departmentId)
        {
            var branches = await _context.Branches
                .Where(b => b.DepartmentId == departmentId && b.IsActive && b.ParentBranchId == null)
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            return Json(branches);
        }

        private bool BranchExists(int id)
        {
            return _context.Branches.Any(e => e.Id == id);
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
                .Select(d => new {
                    d.Id,
                    Name = $"{d.College!.University!.Name} → {d.College!.Name} → {d.Name}"
                })
                .ToListAsync();

            // Ensure we always have a valid SelectList, even if empty
            ViewBag.DepartmentId = departments.Any()
                ? new SelectList(departments, "Id", "Name")
                : new SelectList(new List<object>(), "Id", "Name");
        }

        private async Task PopulateParentBranchesDropdown()
        {
            var parentBranches = await _context.Branches
                .Include(b => b.Department)
                .Where(b => b.IsActive && b.ParentBranchId == null)
                .OrderBy(b => b.Department!.Name)
                .ThenBy(b => b.Name)
                .Select(b => new {
                    b.Id,
                    Name = $"{b.Department!.Name} → {b.Name}"
                })
                .ToListAsync();

            // Ensure we always have a valid SelectList, even if empty
            ViewBag.ParentBranchId = parentBranches.Any()
                ? new SelectList(parentBranches, "Id", "Name")
                : new SelectList(new List<object>(), "Id", "Name");
        }
        
        
        // GET: Branches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches
                .Include(b => b.Department!)
                    .ThenInclude(d => d.College!)
                        .ThenInclude(c => c.University)
                .Include(b => b.ParentBranch)
                .Include(b => b.SubBranches)
                .Include(b => b.BranchSemesters)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (branch == null) return NotFound();

            return View(branch);
        }

        // GET: Branches/Create
        public async Task<IActionResult> Create(int? departmentId, int? parentBranchId)
        {
            var branch = new Branch();

            // Always initialize ViewBag properties to avoid null references
            ViewBag.DepartmentId = new SelectList(new List<object>(), "Value", "Text");
            ViewBag.ParentBranchId = new SelectList(new List<object>(), "Value", "Text");

            if (departmentId.HasValue)
            {
                var department = await _context.Departments
                    .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                    .FirstOrDefaultAsync(d => d.Id == departmentId);
                ViewBag.ParentDepartment = department;

                // Set the department ID for main branches
                branch.DepartmentId = departmentId.Value;

                // Populate departments dropdown with the specific department pre-selected
                await PopulateDepartmentsDropdown(departmentId.Value);
            }
            else if (parentBranchId.HasValue)
            {
                var parentBranch = await _context.Branches
                    .Include(b => b.Department!)
                    .ThenInclude(d => d.College!)
                    .ThenInclude(c => c.University!)
                    .FirstOrDefaultAsync(b => b.Id == parentBranchId);
                ViewBag.ParentBranch = parentBranch;

                // For sub-branches, inherit the department from parent branch
                if (parentBranch != null)
                {
                    branch.DepartmentId = parentBranch.DepartmentId;
                    branch.ParentBranchId = parentBranchId.Value;

                    // Populate departments dropdown with parent's department pre-selected
                    await PopulateDepartmentsDropdown(parentBranch.DepartmentId);
                }
                else
                {
                    await PopulateDepartmentsDropdown();
                }

                await PopulateParentBranchesDropdown(branch.DepartmentId);
            }
            else
            {
                // No parent specified - populate normally
                await PopulateDepartmentsDropdown();
                await PopulateParentBranchesDropdown();
            }

            return View(branch);
        }

        // Updated helper methods with optional parameters
        private async Task PopulateDepartmentsDropdown(int? selectedDepartmentId = null)
        {
            var departments = await _context.Departments
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Where(d => d.IsActive)
                .OrderBy(d => d.College!.University!.Name)
                .ThenBy(d => d.College!.Name)
                .ThenBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.College!.University!.Name} → {d.College!.Name} → {d.Name}"
                })
                .ToListAsync();

            if (selectedDepartmentId.HasValue)
            {
                ViewBag.DepartmentId = new SelectList(departments, "Value", "Text", selectedDepartmentId.Value);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(departments, "Value", "Text");
            }
        }

        private async Task PopulateParentBranchesDropdown(int? departmentId = null)
        {
            IQueryable<Branch> query = _context.Branches
                .Include(b => b.Department)
                .Where(b => b.IsActive && b.ParentBranchId == null);

            if (departmentId.HasValue)
            {
                query = query.Where(b => b.DepartmentId == departmentId.Value);
            }

            var parentBranches = await query
                .OrderBy(b => b.Department!.Name)
                .ThenBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.Department!.Name} → {b.Name}"
                })
                .ToListAsync();

            ViewBag.ParentBranchId = new SelectList(parentBranches, "Value", "Text");
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,BranchCode,Description,DepartmentId,ParentBranchId,IsActive")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                // Validate that DepartmentId is set
                if (branch.DepartmentId == 0)
                {
                    ModelState.AddModelError("DepartmentId", "Department is required.");
                    await PopulateDepartmentsDropdown();
                    await PopulateParentBranchesDropdown();
                    return View(branch);
                }

                // For sub-branches, ensure they belong to the same department as parent
                if (branch.ParentBranchId.HasValue)
                {
                    var parentBranch = await _context.Branches
                        .FirstOrDefaultAsync(b => b.Id == branch.ParentBranchId.Value);

                    if (parentBranch != null && parentBranch.DepartmentId != branch.DepartmentId)
                    {
                        // Auto-correct: set to parent's department
                        branch.DepartmentId = parentBranch.DepartmentId;
                    }
                }

                // Check if branch name already exists in the same department
                var existingBranch = await _context.Branches
                    .AnyAsync(b => b.DepartmentId == branch.DepartmentId &&
                                  b.Name == branch.Name &&
                                  b.IsActive);

                if (existingBranch)
                {
                    ModelState.AddModelError("Name", "A branch with this name already exists in the selected department.");
                    await PopulateDepartmentsDropdown();
                    await PopulateParentBranchesDropdown();
                    return View(branch);
                }

                try
                {
                    _context.Add(branch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Branch '{branch.Name}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating branch");
                    ModelState.AddModelError("", "An error occurred while saving the branch. Please check the department selection.");
                    await PopulateDepartmentsDropdown();
                    await PopulateParentBranchesDropdown();
                    return View(branch);
                }
            }

            await PopulateDepartmentsDropdown();
            await PopulateParentBranchesDropdown();
            return View(branch);
        }

        // POST: Branches/AddSubBranch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubBranch(Branch newSubBranch)
        {
            if (ModelState.IsValid)
            {
                // Check if branch name already exists in the same department
                var existingBranch = await _context.Branches
                    .AnyAsync(b => b.DepartmentId == newSubBranch.DepartmentId &&
                                  b.Name == newSubBranch.Name &&
                                  b.IsActive);

                if (existingBranch)
                {
                    TempData["ErrorMessage"] = "A branch with this name already exists in this department.";
                    return RedirectToAction("ManageSubBranches", new { id = newSubBranch.ParentBranchId });
                }

                _context.Add(newSubBranch);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Sub-branch '{newSubBranch.Name}' added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid sub-branch data.";
            }

            return RedirectToAction("ManageSubBranches", new { id = newSubBranch.ParentBranchId });
        }

        // GET: Branches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .Include(b => b.Department!)
                    .ThenInclude(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Include(b => b.ParentBranch)
                .Include(b => b.SubBranches)
                .Include(b => b.BranchSemesters)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (branch == null)
            {
                return NotFound();
            }

            // Check if branch has sub-branches
            if (branch.SubBranches.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete branch because it has associated sub-branches. Delete the sub-branches first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if branch has semesters
            if (branch.BranchSemesters.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete branch because it has associated semesters. Delete the semesters first.";
                return RedirectToAction(nameof(Index));
            }

            return View(branch);
        }

        // POST: Branches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.SubBranches)
                .Include(b => b.BranchSemesters)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
            {
                return NotFound();
            }

            // Check if branch has sub-branches
            if (branch.SubBranches.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete branch because it has associated sub-branches. Delete the sub-branches first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if branch has semesters
            if (branch.BranchSemesters.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete branch because it has associated semesters. Delete the semesters first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Branch '{branch.Name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
   


}



