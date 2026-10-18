namespace Microclimate.Client.Core.Services;

public interface INavigationService
{
    Task GoToDashboardAsync();
    Task GoToLoginAsync();
    Task GoToSettingsAsync();
    Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null);
    Task GoBackAsync();
}
