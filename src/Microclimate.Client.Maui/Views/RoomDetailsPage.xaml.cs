using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.Views;

public partial class RoomDetailsPage : ContentPage, IQueryAttributable
{
    private readonly RoomDetailsViewModel viewModel;
    private readonly PageLiveUpdates live;
    public RoomDetailsViewModel ViewModel => viewModel;
    private int chartColumns;

    public RoomDetailsPage(RoomDetailsViewModel viewModel, ClientRealtime realtime, INavigationService navigation)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
        live = new PageLiveUpdates(
            realtime,
            viewModel,
            navigation,
            () => viewModel.RoomId is { } id ? new[] { id } : Array.Empty<Guid>(),
            false,
            viewModel.RefreshLiveAsync
        );
    }

    private void OnChartsSizeChanged(object? sender, EventArgs e)
    {
        if (RoomScroll.Width <= 0 || RoomScroll.Height <= 0 || ChartsGrid.Children.Count == 0)
            return;
        var width = RoomScroll.Width - RoomContent.Padding.HorizontalThickness;
        var columns =
            width >= 1080 ? 3
            : width >= 700 ? 2
            : 1;
        var rows = (ChartsGrid.Children.Count + columns - 1) / columns;
        if (chartColumns != columns)
        {
            chartColumns = columns;
            ChartsGrid.ColumnDefinitions.Clear();
            ChartsGrid.RowDefinitions.Clear();
            for (var column = 0; column < columns; column++)
                ChartsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var row = 0; row < rows; row++)
                ChartsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            for (var index = 0; index < ChartsGrid.Children.Count; index++)
            {
                ChartsGrid.SetRow(ChartsGrid.Children[index], index / columns);
                ChartsGrid.SetColumn(ChartsGrid.Children[index], index % columns);
            }
        }
        var visible = RoomContent.Children.OfType<VisualElement>().Where(child => child.IsVisible).ToArray();
        var otherHeight = visible.Where(child => child != ChartsGrid).Sum(child => Math.Max(0, child.Height));
        var available =
            RoomScroll.Height
            - RoomContent.Padding.VerticalThickness
            - RoomContent.Spacing * (visible.Length - 1)
            - otherHeight;
        var height = Math.Max(rows * 320 + (rows - 1) * ChartsGrid.RowSpacing, available);
        if (Math.Abs(ChartsGrid.HeightRequest - height) > 1)
            ChartsGrid.HeightRequest = height;
    }

    protected override bool OnBackButtonPressed()
    {
        if (!viewModel.IsThresholdEditorOpen)
            return base.OnBackButtonPressed();
        viewModel.CloseThresholdsCommand.Execute(null);
        return true;
    }

    // Витягує RoomId і RoomName з параметрів Shell; порожній або некоректний ідентифікатор означає відсутність вибору.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var roomId =
            query.TryGetValue("RoomId", out var id) && id is Guid value && value != Guid.Empty ? value : (Guid?)null;
        var name = query.TryGetValue("RoomName", out var title) ? title as string : null;
        viewModel.SelectRoom(roomId, name);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        live.Start(Window);
        await viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        live.Stop();
        viewModel.CancelPending();
        viewModel.Thresholds.CancelPending();
        base.OnDisappearing();
    }
}
