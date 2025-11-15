using Microsoft.AspNetCore.SignalR;

namespace DentalManagementSystem.Controllers;

public class NotificationHub : Hub
{
    public async Task SendNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    public async Task PatientSentToDoctor(object queueData)
    {
        await Clients.All.SendAsync("PatientSentToDoctor", queueData);
    }

    public async Task PatientCompleted(Guid appointmentId, string patientName, object queueData)
    {
        await Clients.All.SendAsync("PatientCompleted", appointmentId, patientName, queueData);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    }
}
