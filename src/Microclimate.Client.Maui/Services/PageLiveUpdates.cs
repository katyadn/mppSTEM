using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Services;

// Події SignalR приходять поза UI-потоком; тільки цей адаптер змінює стан MAUI-сторінки.
public sealed class PageLiveUpdates(ClientRealtime realtime, BaseViewModel viewModel, INavigationService navigation,
    Func<IReadOnlyCollection<Guid>> rooms, bool includeAlerts, Func<Task> refresh)
{
    private CancellationTokenSource? lifetime;
    private Task running = Task.CompletedTask;
    private Window? window;

    public void Start(Window? currentWindow)
    {
        Stop();
        window = currentWindow;
        if (window is not null) { window.Stopped += OnStopped; window.Resumed += OnResumed; }
        Begin();
    }

    public void Stop()
    {
        lifetime?.Cancel(); lifetime = null;
        if (window is not null) { window.Stopped -= OnStopped; window.Resumed -= OnResumed; window = null; }
    }

    private void OnStopped(object? sender, EventArgs e) { lifetime?.Cancel(); lifetime = null; viewModel.CancelPending(); }
    private void OnResumed(object? sender, EventArgs e) => Begin();
    private void Begin()
    {
        lifetime?.Cancel();
        var source = new CancellationTokenSource();
        lifetime = source;
        running = ObserveAsync(running, source);
    }

    private async Task ObserveAsync(Task previous, CancellationTokenSource source)
    {
        using (source)
        {
            try
            {
                await previous;
                if (source.IsCancellationRequested) return;
                var connection = realtime.CreateConnection();
                await realtime.WatchAsync(connection,
                    () => MainThread.InvokeOnMainThreadAsync(rooms), includeAlerts,
                    () => MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (source.IsCancellationRequested || viewModel.IsBusy) return false;
                        // Під час збереження порогів не запускаємо паралельну аналітику.
                        if (viewModel is RoomDetailsViewModel details && details.Thresholds.IsBusy) return false;
                        await refresh(); return true;
                    }),
                    state => MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (source.IsCancellationRequested) return;
                        viewModel.LiveStatus = state switch
                        {
                            LiveConnectionState.Connected => "Дані оновлюються автоматично",
                            LiveConnectionState.Reconnecting => "Відновлення з’єднання…",
                            LiveConnectionState.SignedOut => "Сесія завершилася",
                            LiveConnectionState.Connecting => "Підключення до автооновлення…",
                            _ => "Канал подій недоступний · перевірка даних кожні 30 с",
                        };
                        if (state == LiveConnectionState.SignedOut) await navigation.GoToLoginAsync();
                    }), source.Token);
            }
            catch (OperationCanceledException) when (source.IsCancellationRequested) { }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Live updates stopped: {exception.GetType().Name}");
                if (!source.IsCancellationRequested) viewModel.LiveStatus = "Автооновлення недоступне. Натисніть «Оновити».";
            }
            finally { if (ReferenceEquals(lifetime, source)) lifetime = null; }
        }
    }
}
