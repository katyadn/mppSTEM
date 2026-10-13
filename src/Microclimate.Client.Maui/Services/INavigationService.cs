namespace Microclimate.Client.Maui.Services;

public interface INavigationService
{
    Task GoToDashboardAsync();
    Task GoToLoginAsync();
    Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null);
    Task GoBackAsync();
}
