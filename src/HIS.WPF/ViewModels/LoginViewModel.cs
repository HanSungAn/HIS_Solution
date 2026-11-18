// [DevExpress.Mvvm] DevExpress에서 제공하는 무료 MVVM 프레임워크 네임스페이스
using System.Windows;
using DevExpress.Mvvm;
using HIS.Services;
using Microsoft.Data.SqlClient;

namespace HIS.WPF.ViewModels;

/// <summary>
/// [DevExpress] ViewModelBase: INotifyPropertyChanged를 자동 구현하는 기본 ViewModel 클래스.
/// 일반 WPF에서는 OnPropertyChanged(nameof(...))를 직접 구현해야 하지만,
/// DevExpress에서는 GetValue / SetValue 메서드로 프로퍼티 변경 알림을 자동 처리합니다.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    public string LoginId
    {
        // [DevExpress] ViewModelBase 내부 저장소에서 프로퍼티 값을 조회
        get => GetValue<string>();
        // [DevExpress] 값을 저장하고 UI에 PropertyChanged 알림을 자동 발생
        set => SetValue(value);
    }

    public string Password
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public string ErrorMessage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    // [DevExpress] AsyncCommand: 비동기(async Task) 메서드를 WPF ICommand로 바인딩할 때 사용.
    // 실행 중 중복 클릭 방지(CanExecute 자동 제어) 기능도 내장되어 있습니다.
    public AsyncCommand LoginCommand { get; }
    public event Action? LoginSucceeded;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginId = "doc1";
        Password = "1234";
        // [DevExpress] 비동기 로그인 메서드를 커맨드로 래핑
        LoginCommand = new AsyncCommand(ExecuteLoginAsync);
    }

    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(LoginId) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "아이디와 비밀번호를 입력하세요.";
            return;
        }

        try
        {
            var result = await _authService.LoginAsync(LoginId.Trim(), Password, forceLogin: false);
            if (result.IsDuplicateSession)
            {
                var confirm = MessageBox.Show(
                    result.Message ?? "이미 다른 곳에서 로그인 중인 계정입니다. 기존 세션을 종료하고 로그인하시겠습니까?",
                    "중복 로그인 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    ErrorMessage = "중복 로그인으로 인해 로그인이 취소되었습니다.";
                    return;
                }

                result = await _authService.LoginAsync(LoginId.Trim(), Password, forceLogin: true);
            }

            if (!result.Success)
            {
                ErrorMessage = result.Message ?? "아이디 또는 비밀번호가 올바르지 않습니다.";
                return;
            }

            LoginSucceeded?.Invoke();
        }
        catch (SqlException ex)
        {
            ErrorMessage = "데이터베이스 연결에 실패했습니다.";
            MessageBox.Show(
                //$"데이터베이스에 연결할 수 없습니다.\nSQL Server 실행 여부와 appsettings.json 연결 문자열을 확인하세요.\n\n상세: {ex.Message}",
                ErrorMessage,
                "DB 연결 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Console.WriteLine( $"DB 연결 오류: {ex.Message}" );
        }
        catch (Exception ex)
        {
            ErrorMessage = "로그인 처리 중 오류가 발생했습니다.";
            MessageBox.Show(
                //$"로그인 중 오류가 발생했습니다.\n\n{ex.Message}",
                ErrorMessage,
                "로그인 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            Console.WriteLine( $"로그인 오류: {ex.Message}" );
        }
    }
}
