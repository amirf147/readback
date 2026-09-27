using System.Windows;
using System.Windows.Input;
using ReadBack.App.ViewModels;

namespace ReadBack.App.Views;

public partial class MiniFloatingHud : Window
{
    private readonly HudViewModel _viewModel;

    public MiniFloatingHud(HudViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HudViewModel.IsVisible))
            {
                Dispatcher.Invoke(() =>
                {
                    if (_viewModel.IsVisible)
                    {
                        PositionTopCenter();
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                });
            }
        };

        Loaded += (s, e) => PositionTopCenter();
    }

    private void PositionTopCenter()
    {
        // Position top center of the screen, similar to Windows 11 Dictation (Win+H)
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
        Top = 35; // Slight offset from top edge
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
