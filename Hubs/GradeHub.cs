using Microsoft.AspNetCore.SignalR;

namespace StudentManagementSystem.Hubs
{
    public class GradeHub : Hub
    {
        public async Task SendGradeUpdate(string courseCode, string message)
        {
            await Clients.All.SendAsync("ReceiveGradeUpdate", courseCode, message);
        }

        public async Task JoinCourseGroup(string courseCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, courseCode);
        }

        public async Task LeaveCourseGroup(string courseCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, courseCode);
        }
    }
}