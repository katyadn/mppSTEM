namespace Microclimate.Client.Maui.Services;

public sealed class NavigationService : INavigationService
{
    public Task GoToDashboardAsync() => NavigateAsync("//main/dashboard/dashboard-page");
    public Task GoToLoginAsync() => NavigateAsync("//auth/sign-in/login");
    public Task GoToRoomDetailsAsync() => NavigateAsync("room-details");
    public Task GoBackAsync() => NavigateAsync("..");

    private static Task NavigateAsync(string route) =>
        Shell.Current.GoToAsync(route);
}
