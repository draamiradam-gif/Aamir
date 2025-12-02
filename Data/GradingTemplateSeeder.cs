using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data
{
    public static class GradingTemplateSeeder
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (!context.GradingTemplates.Any())
            {
                var templates = new List<GradingTemplate>
                {
                    new GradingTemplate
                    {
                        TemplateName = "Standard 60-20-20 Template",
                        Description = "Standard grading: 60% Final Exam, 20% Midterm, 20% Course Work",
                        IsDefault = true,
                        Components = new List<GradingTemplateComponent>
                        {
                            new() { Name = "Final Exam", ComponentType = GradingComponentType.FinalExam, WeightPercentage = 60, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Midterm Exam", ComponentType = GradingComponentType.MidtermExam, WeightPercentage = 20, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Course Work", ComponentType = GradingComponentType.CourseWork, WeightPercentage = 20, MaximumMarks = 100, IsRequired = true }
                        }
                    },
                    new GradingTemplate
                    {
                        TemplateName = "Comprehensive Assessment Template",
                        Description = "Comprehensive assessment with multiple components",
                        IsDefault = false,
                        Components = new List<GradingTemplateComponent>
                        {
                            new() { Name = "Final Exam", ComponentType = GradingComponentType.FinalExam, WeightPercentage = 40, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Midterm Exam", ComponentType = GradingComponentType.MidtermExam, WeightPercentage = 20, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Quizzes", ComponentType = GradingComponentType.Quiz, WeightPercentage = 15, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Assignments", ComponentType = GradingComponentType.Assignment, WeightPercentage = 15, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Participation", ComponentType = GradingComponentType.Participation, WeightPercentage = 10, MaximumMarks = 100, IsRequired = true }
                        }
                    },
                    new GradingTemplate
                    {
                        TemplateName = "Lab-Based Course Template",
                        Description = "Template for laboratory-based courses",
                        IsDefault = false,
                        Components = new List<GradingTemplateComponent>
                        {
                            new() { Name = "Final Exam", ComponentType = GradingComponentType.FinalExam, WeightPercentage = 30, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Practical Exam", ComponentType = GradingComponentType.PracticalExam, WeightPercentage = 30, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Laboratory Work", ComponentType = GradingComponentType.Laboratory, WeightPercentage = 30, MaximumMarks = 100, IsRequired = true },
                            new() { Name = "Lab Reports", ComponentType = GradingComponentType.Assignment, WeightPercentage = 10, MaximumMarks = 100, IsRequired = true }
                        }
                    }
                };

                context.GradingTemplates.AddRange(templates);
                context.SaveChanges();
            }
        }
    }
}