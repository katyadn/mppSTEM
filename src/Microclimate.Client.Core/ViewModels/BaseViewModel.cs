using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

// Спільний стан екрана: завантаження, помилки, скасування та перехід до входу після HTTP 401.
public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    private CancellationTokenSource? pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    // Повідомлення поточної помилки; зміна також оновлює HasError.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string liveStatus = "Підключення до автооновлення…";

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public virtual Task InitializeAsync() => Task.CompletedTask;

    // Запитує скасування активної операції без показу помилки користувачеві.
    public void CancelPending() => pending?.Cancel();

    // Виконує одну операцію за раз, керує Busy та помилками; власне скасування ігнорує, а HTTP 401 за потреби відкриває вхід.
    protected async Task RunAsync(Func<CancellationToken, Task> operation, bool redirectUnauthorized = true)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        using var cancellation = new CancellationTokenSource();
        pending = cancellation;
        try
        {
            await operation(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (ClientApiException exception)
        {
            ErrorMessage = exception.Message;
            if (redirectUnauthorized && exception.StatusCode == HttpStatusCode.Unauthorized)
                await Navigation.GoToLoginAsync();
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            pending = null;
            IsBusy = false;
        }
    }
}
