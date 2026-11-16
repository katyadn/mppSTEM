namespace Microclimate.Client.Core.Services;

// Абстракція переходів між екранами, доступна ViewModel без залежності від MAUI Shell.
public interface INavigationService
{
    Task GoToDashboardAsync();

    Task GoToLoginAsync();

    Task GoToSettingsAsync();

    Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null);

    Task GoBackAsync();
}
