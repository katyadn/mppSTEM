namespace Microclimate.Client.Maui.Services;

public interface INavigationService
{
    Task GoToDashboardAsync();
    Task GoToLoginAsync();
    Task GoToRoomDetailsAsync();
    Task GoBackAsync();
}
