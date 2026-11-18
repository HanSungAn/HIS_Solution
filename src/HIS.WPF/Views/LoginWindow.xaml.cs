using System.Windows;
using System.Windows.Controls;
using HIS.WPF.ViewModels;

namespace HIS.WPF.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        PwdBox.Password = _vm.Password;
        _vm.LoginSucceeded += () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _vm.Password = pb.Password;
    }
}
