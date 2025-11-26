using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.Table.PivotTable;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers
{
    public class ComprehensiveGradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IComprehensiveGradeService _comprehensiveGradeService;
        private readonly IComprehensiveGradeService _gradeService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ComprehensiveGradeService> _logger;

        public ComprehensiveGradesController(ApplicationDbContext context, IComprehensiveGradeService comprehensiveGradeService,
                                           IComprehensiveGradeService gradeService,
                                           IWebHostEnvironment environment, ILogger<ComprehensiveGradeService> logger)
        {
            _context = context;
            _comprehensiveGradeService = comprehensiveGradeService;
            _gradeService = gradeService;
            _environment = environment;
            _logger = logger;
        }

        // GET: Main grading dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new GradingDashboardViewModel
            {
                RecentEvaluations = await _context.CourseEvaluations
                    .Include(e => e.Course)
                    .Include(e => e.EvaluationType)
                    .Where(e => e.EvaluationDate >= DateTime.Now.AddDays(-7))
                    .OrderByDescending(e => e.EvaluationDate)
                    .Take(10)
                    .ToListAsync(),

                UpcomingEvaluations = await _context.CourseEvaluations
                    .Include(e => e.Course)
                    .Include(e => e.EvaluationType)
                    .Where(e => e.EvaluationDate >= DateTime.Now && e.EvaluationDate <= DateTime.Now.AddDays(30))
                    .OrderBy(e => e.EvaluationDate)
                    .Take(10)
                    .ToListAsync(),

                GradingTemplates = await _context.GradingTemplates
                    .Include(t => t.Items)
                    .ThenInclude(i => i.EvaluationType)
                    .Where(t => t.IsActive)
                    .ToListAsync()
            };

            return View(model);
        }

        // GET: Course evaluations selection
        [HttpGet]
        [Route("ComprehensiveGrades/CourseEvaluations")]
        public async Task<IActionResult> CourseEvaluations()
        {
            try
            {
                var courses = await _context.Courses
                    .AsNoTracking()  // Add this
                    .Where(c => c.IsActive && c.Id > 0)  // Ensure valid IDs
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                if (!courses.Any())
                {
                    TempData["Warning"] = "No active courses found with valid IDs";
                }

                return View("SelectCourse", courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                TempData["Error"] = "Error loading courses";
                return View("SelectCourse", new List<Course>());
            }
        }

        // GET: Course evaluations for specific course
        [HttpGet]
        [Route("ComprehensiveGrades/CourseEvaluations/{courseId:int}")]
        public async Task<IActionResult> CourseEvaluations(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.CourseEvaluations)
                .ThenInclude(e => e.EvaluationType)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound();
            }

            ViewBag.Course = course;
            ViewBag.EvaluationTypes = await _context.EvaluationTypes!
                .Where(et => et.IsActive)
                .OrderBy(et => et.Order)
                .ToListAsync();

            return View("CourseEvaluationsList", course.CourseEvaluations.ToList());
        }


        // GET: ComprehensiveGrades/CourseFinalGrades
        public async Task<IActionResult> CourseFinalGrades(int? courseId)
        {
            if (courseId.HasValue)
            {
                await _gradeService.CalculateAllFinalGradesAsync(courseId.Value);
                var enrollments = await _context.CourseEnrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .Where(e => e.CourseId == courseId.Value && e.IsActive)
                    .ToListAsync();

                ViewBag.Course = await _context.Courses.FindAsync(courseId.Value);
                return View(enrollments);
            }

            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseName)
                .ToListAsync();
            return View("SelectCourseForFinalGrades", courses);
        }


        // GET: Import grades selection
        [HttpGet]
        [Route("ComprehensiveGrades/ImportGrades")]
        public async Task<IActionResult> ImportGrades()
        {
            try
            {
                var evaluations = await _context.CourseEvaluations
                    .Include(e => e.Course)
                    .Include(e => e.EvaluationType)
                    .Where(e => !e.IsGraded)
                    .OrderBy(e => e.DueDate)
                    .ToListAsync();

                return View("SelectEvaluation", evaluations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evaluations for import");
                TempData["Error"] = "Error loading evaluations. Please try again.";
                return View("SelectEvaluation", new List<CourseEvaluation>());
            }
        }

        // GET: Import grades for specific evaluation
        [HttpGet]
        [Route("ComprehensiveGrades/ImportGrades/{evaluationId:int}")]
        public async Task<IActionResult> ImportGrades(int evaluationId)
        {
            var evaluation = await _context.CourseEvaluations
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation == null)
            {
                return NotFound();
            }

            var model = new ImportGradesViewModel
            {
                EvaluationId = evaluationId,
                EvaluationName = evaluation.Title,
                CourseName = evaluation.Course?.CourseName ?? "Unknown Course"
            };

            return View(model);
        }

        // GET: Course final grades selection
        [HttpGet]
        [Route("ComprehensiveGrades/CourseFinalGrades")]
        public async Task<IActionResult> CourseFinalGrades()
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View("SelectCourseForFinalGrades", courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for final grades");
                TempData["Error"] = "Error loading courses. Please try again.";
                return View("SelectCourseForFinalGrades", new List<Course>());
            }
        }

        // GET: Course final grades for specific course
        [HttpGet]
        [Route("ComprehensiveGrades/CourseFinalGrades/{courseId:int}")]
        public async Task<IActionResult> CourseFinalGrades(int courseId)
        {
            await _gradeService.CalculateAllFinalGradesAsync(courseId);
            var enrollments = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Where(e => e.CourseId == courseId && e.IsActive)
                .ToListAsync();

            ViewBag.Course = await _context.Courses.FindAsync(courseId);
            return View(enrollments); // Return List<CourseEnrollment> directly
        }

        // GET: Grade students for an evaluation
        [HttpGet]
        [Route("ComprehensiveGrades/GradeEvaluation/{evaluationId:int}")]
        public async Task<IActionResult> GradeEvaluation(int evaluationId)
        {
            var evaluation = await _context.CourseEvaluations
                .Include(e => e.Course)
                .Include(e => e.EvaluationType)
                .Include(e => e.StudentGrades)
                .ThenInclude(sg => sg.Student)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation == null)
            {
                return NotFound();
            }

            // Get students enrolled in the course
            var enrolledStudents = await _context.CourseEnrollments
                .Include(ce => ce.Student)
                .Where(ce => ce.CourseId == evaluation.CourseId && ce.IsActive)
                .ToListAsync();

            var model = new GradeEvaluationViewModel
            {
                Evaluation = evaluation,
                Students = enrolledStudents.Select(es => new StudentGradeEntry
                {
                    StudentId = es.StudentId,
                    StudentName = es.Student?.Name ?? "Unknown",
                    StudentNumber = es.Student?.StudentId ?? "N/A",
                    ExistingGrade = evaluation.StudentGrades.FirstOrDefault(sg => sg.StudentId == es.StudentId)
                }).ToList()
            };

            return View(model);
        }

        // POST: Create new evaluation
        [HttpPost]
        [Route("ComprehensiveGrades/CreateEvaluation")]
        public async Task<IActionResult> CreateEvaluation(CourseEvaluation evaluation)
        {
            try
            {
                evaluation.Semester = "Fall 2024";
                var createdEvaluation = await _gradeService.CreateEvaluationAsync(evaluation);

                TempData["Success"] = $"Evaluation '{createdEvaluation.Title}' created successfully";
                return RedirectToAction(nameof(CourseEvaluations), new { courseId = evaluation.CourseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating evaluation: {ex.Message}";
                return RedirectToAction(nameof(CourseEvaluations), new { courseId = evaluation.CourseId });
            }
        }

        // POST: Save grades for evaluation
        [HttpPost]
        [Route("ComprehensiveGrades/SaveGrades/{evaluationId:int}")]
        public async Task<IActionResult> SaveGrades(int evaluationId, List<StudentGradeEntry> grades)
        {
            try
            {
                var studentGrades = grades.Select(g => new StudentGrade
                {
                    StudentId = g.StudentId,
                    CourseEvaluationId = evaluationId,
                    Score = g.Score,
                    MaxScore = g.MaxScore,
                    Comments = g.Comments,
                    IsAbsent = g.IsAbsent,
                    IsExcused = g.IsExcused,
                    ExcuseReason = g.ExcuseReason,
                    GradedBy = User.Identity?.Name ?? "System",
                    GradedDate = DateTime.Now
                }).ToList();

                var result = await _gradeService.BulkAssignGradesAsync(studentGrades);

                if (result)
                {
                    TempData["Success"] = $"Grades saved for {studentGrades.Count} students";
                }
                else
                {
                    TempData["Error"] = "Failed to save grades";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error saving grades: {ex.Message}";
            }

            return RedirectToAction(nameof(GradeEvaluation), new { evaluationId });
        }

        // POST: Import grades from Excel
        [HttpPost]
        [Route("ComprehensiveGrades/ImportGrades/{evaluationId:int}")]
        public async Task<IActionResult> ImportGrades(int evaluationId, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Please select an Excel file";
                return RedirectToAction(nameof(ImportGrades), new { evaluationId });
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                var result = await _gradeService.ImportGradesFromExcelAsync(stream, evaluationId);

                if (result)
                {
                    TempData["Success"] = "Grades imported successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to import grades";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error importing grades: {ex.Message}";
            }

            return RedirectToAction(nameof(GradeEvaluation), new { evaluationId });
        }

        // POST: Calculate all final grades
        [HttpPost]
        [Route("ComprehensiveGrades/CalculateFinalGrades/{courseId:int}")]
        public async Task<IActionResult> CalculateFinalGrades(int courseId)
        {
            var result = await _gradeService.CalculateAllFinalGradesAsync(courseId);

            if (result)
            {
                TempData["Success"] = "Final grades calculated successfully";
            }
            else
            {
                TempData["Error"] = "Failed to calculate final grades";
            }

            return RedirectToAction(nameof(CourseFinalGrades), new { courseId });
        }




        // GET: Grade distribution
        [HttpGet]
        [Route("ComprehensiveGrades/GradeDistribution")]
        public async Task<IActionResult> GradeDistribution()
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View("SelectCourseForGradeDistribution", courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for grade distribution");
                TempData["Error"] = "Error loading courses. Please try again.";
                return View("SelectCourseForGradeDistribution", new List<Course>());
            }
        }

        // GET: Grade distribution for specific course
        [HttpGet]
        [Route("ComprehensiveGrades/GradeDistribution/{courseId:int}")]
        public async Task<IActionResult> GradeDistribution(int courseId)
        {
            var distribution = await _gradeService.GetGradeDistributionAsync(courseId);
            var course = await _context.Courses.FindAsync(courseId);

            var model = new GradeDistributionViewModel
            {
                Distribution = distribution,
                Course = course!
            };

            return View(model);
        }

        // GET: Evaluation statistics selection
        [HttpGet]
        [Route("ComprehensiveGrades/EvaluationStatistics")]
        public async Task<IActionResult> EvaluationStatistics()
        {
            try
            {
                var evaluations = await _context.CourseEvaluations
                    .Include(e => e.Course)
                    .Include(e => e.EvaluationType)
                    .Where(e => e.IsGraded)
                    .OrderBy(e => e.EvaluationDate)
                    .ToListAsync();

                return View("SelectEvaluationForStatistics", evaluations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evaluations for statistics");
                TempData["Error"] = "Error loading evaluations. Please try again.";
                return View("SelectEvaluationForStatistics", new List<CourseEvaluation>());
            }
        }

        // GET: Evaluation statistics for specific evaluation
        [HttpGet]
        [Route("ComprehensiveGrades/EvaluationStatistics/{evaluationId:int}")]
        public async Task<IActionResult> EvaluationStatistics(int evaluationId)
        {
            var statistics = await _gradeService.GetEvaluationStatisticsAsync(evaluationId);
            var evaluation = await _context.CourseEvaluations
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            var model = new EvaluationStatisticsViewModel
            {
                Statistics = statistics,
                Evaluation = evaluation!
            };

            return View(model);
        }

        // GET: Export reports selection
        [HttpGet]
        [Route("ComprehensiveGrades/ExportReports")]
        public async Task<IActionResult> ExportReports()
        {
            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var model = new ExportReportsViewModel
            {
                Courses = courses,
                ReportTypes = new List<ReportType>
        {
            new ReportType { Id = 1, Name = "Grade Sheet", Description = "Export all grades for a course" },
            new ReportType { Id = 2, Name = "Final Grades", Description = "Export final course grades" },
            new ReportType { Id = 3, Name = "Grade Distribution", Description = "Export grade distribution analytics" },
            new ReportType { Id = 4, Name = "Student Transcript", Description = "Export individual student transcripts" }
        }
            };

            return View(model);
        }

        // POST: Generate and download report
        [HttpPost]
        [Route("ComprehensiveGrades/GenerateReport")]
        public async Task<IActionResult> GenerateReport(ExportRequest request)
        {
            try
            {
                byte[] reportData;
                string contentType;
                string fileName;

                switch (request.ReportTypeId)
                {
                    case 1: // Grade Sheet
                        (reportData, contentType, fileName) = await GenerateGradeSheetReport(request.CourseId);
                        break;
                    case 2: // Final Grades
                        (reportData, contentType, fileName) = await GenerateFinalGradesReport(request.CourseId);
                        break;
                    case 3: // Grade Distribution
                        (reportData, contentType, fileName) = await GenerateGradeDistributionReport(request.CourseId);
                        break;
                    case 4: // Student Transcript
                        (reportData, contentType, fileName) = await GenerateStudentTranscriptReport(request.StudentId);
                        break;
                    default:
                        TempData["Error"] = "Invalid report type selected";
                        return RedirectToAction(nameof(ExportReports));
                }

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating report: {ex.Message}";
                return RedirectToAction(nameof(ExportReports));
            }
        }

        private async Task<(byte[], string, string)> GenerateGradeSheetReport(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.CourseEvaluations)
                .ThenInclude(e => e.StudentGrades)
                .ThenInclude(sg => sg.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                throw new Exception("Course not found");

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Grade Sheet");

            // Header
            worksheet.Cells[1, 1].Value = $"Grade Sheet - {course.CourseName}";
            worksheet.Cells[1, 1, 1, 5].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;

            // Column headers
            worksheet.Cells[3, 1].Value = "Student ID";
            worksheet.Cells[3, 2].Value = "Student Name";

            int col = 3;
            foreach (var evaluation in course.CourseEvaluations.OrderBy(e => e.EvaluationDate))
            {
                worksheet.Cells[3, col].Value = evaluation.Title;
                col++;
            }
            worksheet.Cells[3, col].Value = "Final Grade";

            // Data
            int row = 4;
            var enrollments = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId && e.IsActive)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                worksheet.Cells[row, 1].Value = enrollment.Student?.StudentId;
                worksheet.Cells[row, 2].Value = enrollment.Student?.Name;

                col = 3;
                foreach (var evaluation in course.CourseEvaluations.OrderBy(e => e.EvaluationDate))
                {
                    var grade = evaluation.StudentGrades.FirstOrDefault(sg => sg.StudentId == enrollment.StudentId);
                    worksheet.Cells[row, col].Value = grade?.Score;
                    col++;
                }
                worksheet.Cells[row, col].Value = enrollment.Grade;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return (package.GetAsByteArray(),
                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                   $"GradeSheet_{course.CourseCode}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task<(byte[], string, string)> GenerateFinalGradesReport(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                throw new Exception("Course not found");

            var enrollments = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId && e.IsActive)
                .ToListAsync();

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Final Grades");

            // Header
            worksheet.Cells[1, 1].Value = $"Final Grades - {course.CourseName}";
            worksheet.Cells[1, 1, 1, 4].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            // Column headers
            worksheet.Cells[3, 1].Value = "Student ID";
            worksheet.Cells[3, 2].Value = "Student Name";
            worksheet.Cells[3, 3].Value = "Final Grade";
            worksheet.Cells[3, 4].Value = "Grade Letter";

            // Data
            int row = 4;
            foreach (var enrollment in enrollments.OrderBy(e => e.Student?.Name))
            {
                worksheet.Cells[row, 1].Value = enrollment.Student?.StudentId;
                worksheet.Cells[row, 2].Value = enrollment.Student?.Name;
                worksheet.Cells[row, 3].Value = enrollment.Grade;
                worksheet.Cells[row, 4].Value = enrollment.GradeLetter;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return (package.GetAsByteArray(),
                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                   $"FinalGrades_{course.CourseCode}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task<(byte[], string, string)> GenerateGradeDistributionReport(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                throw new Exception("Course not found");

            var distribution = await _gradeService.GetGradeDistributionAsync(courseId);

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Grade Distribution");

            // Header
            worksheet.Cells[1, 1].Value = $"Grade Distribution - {course.CourseName}";
            worksheet.Cells[1, 1, 1, 3].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            // Statistics
            worksheet.Cells[3, 1].Value = "Total Students";
            worksheet.Cells[3, 2].Value = distribution.TotalStudents;

            worksheet.Cells[4, 1].Value = "Pass Rate";
            worksheet.Cells[4, 2].Value = distribution.PassRate;

            worksheet.Cells[5, 1].Value = "Fail Rate";
            worksheet.Cells[5, 2].Value = distribution.FailRate;

            // Grade distribution
            worksheet.Cells[7, 1].Value = "Grade";
            worksheet.Cells[7, 2].Value = "Count";
            worksheet.Cells[7, 3].Value = "Percentage";

            int row = 8;
            foreach (var (grade, count) in distribution.Distribution)
            {
                worksheet.Cells[row, 1].Value = grade;
                worksheet.Cells[row, 2].Value = count;
                worksheet.Cells[row, 3].Value = (count * 100.0 / distribution.TotalStudents).ToString("0.00") + "%";
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return (package.GetAsByteArray(),
                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                   $"GradeDistribution_{course.CourseCode}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task<(byte[], string, string)> GenerateStudentTranscriptReport(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                throw new Exception("Student not found");

            var transcript = await _gradeService.GenerateTranscriptAsync(studentId);

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Transcript");

            // Header
            worksheet.Cells[1, 1].Value = $"Academic Transcript - {student.Name}";
            worksheet.Cells[1, 1, 1, 5].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[2, 1].Value = "Student ID";
            worksheet.Cells[2, 2].Value = student.StudentId;

            worksheet.Cells[3, 1].Value = "Cumulative GPA";
            worksheet.Cells[3, 2].Value = transcript.GPA;

            // Course history
            worksheet.Cells[5, 1].Value = "Course Code";
            worksheet.Cells[5, 2].Value = "Course Name";
            worksheet.Cells[5, 3].Value = "Credits";
            worksheet.Cells[5, 4].Value = "Grade";
            worksheet.Cells[5, 5].Value = "Grade Points";

            int row = 6;
            foreach (var enrollment in transcript.Enrollments)
            {
                worksheet.Cells[row, 1].Value = enrollment.Course?.CourseCode;
                worksheet.Cells[row, 2].Value = enrollment.Course?.CourseName;
                worksheet.Cells[row, 3].Value = enrollment.Course?.Credits;
                worksheet.Cells[row, 4].Value = enrollment.GradeLetter;
                worksheet.Cells[row, 5].Value = enrollment.GradePoints;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return (package.GetAsByteArray(),
                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                   $"Transcript_{student.StudentId}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: Generate student transcript
        [HttpGet]
        [Route("ComprehensiveGrades/StudentTranscript/{studentId:int}")]
        public async Task<IActionResult> StudentTranscript(int studentId)
        {
            // ✅ USE EXISTING GRADE SERVICE FOR TRANSCRIPT
            var transcript = await _gradeService.GenerateTranscriptAsync(studentId);

            if (transcript?.Student == null)
            {
                TempData["Error"] = "Student not found";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(transcript);
        }

        // Add this to your ComprehensiveGradesController
        public async Task<IActionResult> CheckData()
        {
            var dataCheck = new
            {
                TotalCourses = await _context.Courses.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalEnrollments = await _context.CourseEnrollments.CountAsync(),
                ActiveEnrollments = await _context.CourseEnrollments.CountAsync(e => e.IsActive),
                TotalEvaluations = await _context.CourseEvaluations.CountAsync(),
                GradedEvaluations = await _context.CourseEvaluations.CountAsync(e => e.IsGraded),
                TotalStudentGrades = await _context.StudentGrades.CountAsync()
            };

            ViewBag.DataCheck = dataCheck;
            return View();
        }

        // GET: ComprehensiveGrades/CreateSampleEvaluations
        public async Task<IActionResult> CreateSampleEvaluations()
        {
            try
            {
                var courses = await _context.Courses.Where(c => c.IsActive).Take(3).ToListAsync();
                var evaluationTypes = await _context.EvaluationTypes.Where(et => et.IsActive).Take(3).ToListAsync();

                if (!courses.Any() || !evaluationTypes.Any())
                {
                    TempData["Error"] = "Need active courses and evaluation types first";
                    return RedirectToAction(nameof(Dashboard));
                }

                int evaluationsCreated = 0;

                foreach (var course in courses)
                {
                    foreach (var evalType in evaluationTypes)
                    {
                        // Check if evaluation already exists
                        var existing = await _context.CourseEvaluations
                            .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.EvaluationTypeId == evalType.Id);

                        if (existing == null)
                        {
                            var evaluation = new CourseEvaluation
                            {
                                CourseId = course.Id,
                                EvaluationTypeId = evalType.Id,
                                Title = $"{evalType.Name} - {course.CourseCode}",
                                Description = $"Sample {evalType.Name} for {course.CourseName}",
                                Weight = evalType.DefaultWeight,
                                MaxScore = 100,
                                EvaluationDate = DateTime.Now.AddDays(14),
                                DueDate = DateTime.Now.AddDays(7),
                                IsGraded = false
                            };

                            _context.CourseEvaluations.Add(evaluation);
                            evaluationsCreated++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Created {evaluationsCreated} sample evaluations";
                return RedirectToAction(nameof(CourseEvaluations));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating evaluations: {ex.Message}";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("ComprehensiveGrades/SaveGrades")]
        public async Task<IActionResult> SaveGrades([FromBody] List<StudentGrade> grades)
        {
            try
            {
                var result = await _gradeService.BulkAssignGradesAsync(grades);

                if (result)
                {
                    return Json(new { success = true, message = "Grades saved successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to save grades" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving grades");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}