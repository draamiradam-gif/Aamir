using StudentManagementSystem.Models;
using System.IO;
using System.Threading.Tasks;

namespace StudentManagementSystem.Services
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student?> GetStudentByStudentIdAsync(string studentId);
        Task AddStudentAsync(Student student);
        Task UpdateStudentAsync(Student student);
        Task DeleteStudentAsync(int id);
        Task<bool> StudentExistsAsync(string studentId);

        // Import/Export methods
        Task<ImportResult> AnalyzeExcelImportAsync(Stream stream, ImportSettings? settings = null);
        Task<ImportResult> ExecuteImportAsync(ImportResult analysisResult, ImportSettings settings);
        Task<byte[]> ExportStudentsToExcelAsync();
        Task<byte[]> ExportSelectedStudentsAsync(int[] studentIds); // Add this method
        Task<byte[]> ExportStudentToPdfAsync(int studentId);
        Task<byte[]> ExportAllStudentsToPdfAsync();
        Task<string> AddTestStudents();
    }
}