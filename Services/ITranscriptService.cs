using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface ITranscriptService
    {
        Task<StudentTranscript> GenerateOfficialTranscriptAsync(int studentId);
        Task<StudentTranscript> GenerateUnofficialTranscriptAsync(int studentId);
    }

    public class TranscriptService : ITranscriptService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGradeService _gradeService;

        public TranscriptService(ApplicationDbContext context, IGradeService gradeService)
        {
            _context = context;
            _gradeService = gradeService;
        }

        public async Task<StudentTranscript> GenerateOfficialTranscriptAsync(int studentId)
        {
            return await _gradeService.GenerateTranscriptAsync(studentId);
        }

        public async Task<StudentTranscript> GenerateUnofficialTranscriptAsync(int studentId)
        {
            return await _gradeService.GenerateTranscriptAsync(studentId);
        }
    }
}