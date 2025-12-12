using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers
{
    public class CourseMaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CourseMaterialsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Admin: Manage course materials
        public async Task<IActionResult> Index(int? courseId)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.CourseMaterials
                .Include(cm => cm.Course)
                .AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(cm => cm.CourseId == courseId.Value);
                ViewBag.SelectedCourse = await _context.Courses.FindAsync(courseId.Value);
            }

            ViewBag.Courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            var materials = await query
                .OrderByDescending(cm => cm.UploadDate)
                .ToListAsync();

            return View(materials);
        }

        // Upload material
        [HttpPost]
        public async Task<IActionResult> Upload(CourseMaterial material, IFormFile file)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a file" });
            }

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".txt", ".zip" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "File type not allowed" });
                }

                // Create upload directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "materials");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save material record
                material.FileName = fileName;
                material.FilePath = $"/uploads/materials/{fileName}";
                material.FileSize = file.Length;
                material.UploadDate = DateTime.Now;
                material.UploadedBy = User.FindFirstValue(ClaimTypes.Name) ?? "System";

                _context.CourseMaterials.Add(material);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "File uploaded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Delete material
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var material = await _context.CourseMaterials.FindAsync(id);
            if (material == null)
            {
                return Json(new { success = false, message = "Material not found" });
            }

            try
            {
                // Delete physical file
                var filePath = Path.Combine(_environment.WebRootPath, material.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete database record
                _context.CourseMaterials.Remove(material);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Material deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Update material visibility
        [HttpPost]
        public async Task<IActionResult> UpdateVisibility(int id, bool isVisibleToStudents)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var material = await _context.CourseMaterials.FindAsync(id);
            if (material == null)
            {
                return Json(new { success = false, message = "Material not found" });
            }

            material.IsVisibleToStudents = isVisibleToStudents;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Visibility updated" });
        }

        // Download material
        public async Task<IActionResult> Download(int id)
        {
            var material = await _context.CourseMaterials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            // Check access rights
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Instructor");
            var isStudent = User.IsInRole("Student");

            if (!isAdmin && (!material.IsVisibleToStudents || !isStudent))
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, material.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(filePath), material.OriginalFileName);
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".pdf", "application/pdf"},
                {".doc", "application/msword"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".ppt", "application/vnd.ms-powerpoint"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".png", "image/png"},
                {".txt", "text/plain"},
                {".zip", "application/zip"}
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }
    }
}