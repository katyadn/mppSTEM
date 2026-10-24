using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Maui.Services;

// Перетворює переходи клієнта на маршрути MAUI Shell.
public sealed class NavigationService : INavigationService
{
    // Відкриває головний екран зі списком кімнат.
    public Task GoToDashboardAsync() => NavigateAsync("//main/dashboard/dashboard-page");
    // Відкриває налаштування клієнта.
    public Task GoToSettingsAsync() => NavigateAsync("client-settings");
    // Відкриває екран входу.
    public Task GoToLoginAsync() => NavigateAsync("//auth/sign-in/login");
    // Відкриває деталі кімнати з її ідентифікатором і необов’язковою назвою.
    public Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null) =>
        Shell.Current.GoToAsync("room-details", new Dictionary<string, object>
        {
            ["RoomId"] = roomId ?? Guid.Empty,
            ["RoomName"] = roomName ?? string.Empty,
        });
    // Повертається на попередній екран.
    public Task GoBackAsync() => NavigateAsync("..");

    // Виконує перехід за маршрутом MAUI Shell.
    private static Task NavigateAsync(string route) =>
        Shell.Current.GoToAsync(route);
}
