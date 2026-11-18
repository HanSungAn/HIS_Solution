using System.Windows;
using HIS.WPF.ViewModels;

namespace HIS.WPF;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public bool IsLoggedOut { get; private set; }

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        _vm.LogoutRequested += () =>
        {
            IsLoggedOut = true;
            Close();
        };
        Loaded += async (_, _) => await _vm.LoadInitialDataAsync();
        Closed += async (_, _) =>
        {
            _vm.StopSessionTimer();
            if (!IsLoggedOut)
                await _vm.CleanupSessionOnCloseAsync();
        };
    }
}
