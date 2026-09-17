using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ServiceBooking.Api.Hubs;

[Authorize]
public class BookingHub : Hub
{
  public const string AdminGroup = "admins";

  public static string CustomerGroup(Guid customerId)
      => $"customer:{customerId}";

  public override async Task OnConnectedAsync()
  {
    var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    var role = Context.User?.FindFirstValue(ClaimTypes.Role);

    if (Guid.TryParse(userIdClaim, out var userId))
    {
      if (role == "Admin")
      {
        await Groups.AddToGroupAsync(
          Context.ConnectionId, AdminGroup);
      }
      else if (role == "Customer")
      {
        await Groups.AddToGroupAsync(
          Context.ConnectionId, CustomerGroup(userId));
      }
    }

    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(
      Exception? exception)
  {
    var userIdClaim =
      Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    var role =
      Context.User?.FindFirstValue(ClaimTypes.Role);

    if (Guid.TryParse(userIdClaim, out var userId))
    {
      if (role == "Admin")
      {
        await Groups.RemoveFromGroupAsync(
          Context.ConnectionId,
          AdminGroup);
      }
      else if (role == "Customer")
      {
        await Groups.RemoveFromGroupAsync(
          Context.ConnectionId,
          CustomerGroup(userId));
      }
    }

    await base.OnDisconnectedAsync(exception);
  }
}