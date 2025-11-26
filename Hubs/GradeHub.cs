using Microsoft.AspNetCore.SignalR;

namespace StudentManagementSystem.Hubs
{
    public class GradeHub : Hub
    {
        public async Task SendGradeUpdate(int studentId, string courseName, decimal grade)
        {
            await Clients.All.SendAsync("ReceiveGradeUpdate", studentId, courseName, grade);
        }

        public async Task SendGradeRevisionUpdate(int revisionId, string status)
        {
            await Clients.All.SendAsync("ReceiveGradeRevisionUpdate", revisionId, status);
        }

        public async Task JoinStudentGroup(int studentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"student-{studentId}");
        }

        public async Task JoinCourseGroup(int courseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"course-{courseId}");
        }
    }
}