using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;
    private CancellationTokenSource? pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;
    [ObservableProperty]
    private string title = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public virtual Task InitializeAsync() => Task.CompletedTask;
    public void CancelPending() => pending?.Cancel();

    protected async Task RunAsync(Func<CancellationToken, Task> operation, bool redirectUnauthorized = true)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        using var cancellation = new CancellationTokenSource();
        pending = cancellation;
        try { await operation(cancellation.Token); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (ClientApiException exception)
        {
            ErrorMessage = exception.Message;
            if (redirectUnauthorized && exception.StatusCode == HttpStatusCode.Unauthorized) await Navigation.GoToLoginAsync();
        }
        catch (ArgumentException exception) { ErrorMessage = exception.Message; }
        finally { pending = null; IsBusy = false; }
    }
}
