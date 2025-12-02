using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class EmailTemplatesController : Controller
    {
        private readonly List<EmailTemplateViewModel> _templates = new()
        {
            new EmailTemplateViewModel
            {
                Id = 1,
                TemplateType = "AdminWelcome",
                Subject = "Welcome to Admin Portal",
                Body = "Dear {AdminName}, Welcome to our admin portal. Your temporary password is: {Password}"
            },
            new EmailTemplateViewModel
            {
                Id = 2,
                TemplateType = "ApplicationApproved",
                Subject = "Admin Application Approved",
                Body = "Dear {ApplicantName}, Your admin application has been approved."
            },
            new EmailTemplateViewModel
            {
                Id = 3,
                TemplateType = "ApplicationRejected",
                Subject = "Admin Application Status",
                Body = "Dear {ApplicantName}, Your admin application has been reviewed."
            }
        };

        public async Task<IActionResult> Index()
        {
            // Simulate async operation
            await Task.Delay(50);
            return View(_templates);
        }

        public async Task<IActionResult> EditTemplate(string templateType)
        {
            await Task.Delay(50); // Simulate async operation

            var template = _templates.FirstOrDefault(t => t.TemplateType == templateType);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found";
                return RedirectToAction("Index");
            }

            return View(template);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTemplate(EmailTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await Task.Delay(100); // Simulate async save operation

                var existingTemplate = _templates.FirstOrDefault(t => t.Id == model.Id);
                if (existingTemplate != null)
                {
                    existingTemplate.Subject = model.Subject;
                    existingTemplate.Body = model.Body;
                    existingTemplate.LastModified = DateTime.Now;
                }

                TempData["SuccessMessage"] = "Email template updated successfully!";
                return RedirectToAction("Index");
            }

            return View("EditTemplate", model);
        }

        public async Task<IActionResult> SendTestEmail(string templateType)
        {
            try
            {
                // Simulate email sending
                await Task.Delay(1000);

                return Json(new { success = true, message = $"Test email for {templateType} sent successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to send test email: {ex.Message}" });
            }
        }

        public async Task<IActionResult> PreviewTemplate(string templateType)
        {
            await Task.Delay(50); // Simulate async operation

            var template = _templates.FirstOrDefault(t => t.TemplateType == templateType);
            if (template == null)
            {
                return NotFound();
            }

            // Replace placeholders with sample data
            var previewBody = template.Body
                .Replace("{AdminName}", "John Doe")
                .Replace("{Password}", "TempPassword123!")
                .Replace("{ApplicantName}", "Jane Smith");

            return Content(previewBody, "text/html");
        }
    }
}