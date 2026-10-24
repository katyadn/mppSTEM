namespace Microclimate.Client.Core.Services;

// Абстракція переходів між екранами, доступна ViewModel без залежності від MAUI Shell.
public interface INavigationService
{
    // Відкриває головний екран зі списком кімнат.
    Task GoToDashboardAsync();
    // Відкриває екран входу.
    Task GoToLoginAsync();
    // Відкриває налаштування клієнта.
    Task GoToSettingsAsync();
    // Відкриває деталі кімнати з її ідентифікатором і необов’язковою назвою.
    Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null);
    // Повертається на попередній екран.
    Task GoBackAsync();
}
