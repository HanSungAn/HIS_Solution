using System.Windows;
using System.Windows.Threading;
using HIS.Data;
using HIS.Data.Repositories;
using HIS.Services;
using HIS.WPF.ViewModels;
using HIS.WPF.Views;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HIS.WPF;

/// <summary>
/// 애플리케이션 진입점: DI(의존성 주입) 컨테이너를 구성하고 로그인 성공 시 메인 창을 엽니다.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // UI 스레드에서 처리되지 않은 예외(DB 연결 실패 등) 발생 시 팝업으로 안내
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 1. appsettings.json에서 DB 연결 문자열 로드
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connStr = config.GetConnectionString("HisDb")
            ?? throw new InvalidOperationException("HisDb connection string is missing.");
        var sessionTimeoutMinutes = int.TryParse(config["Auth:SessionTimeoutMinutes"], out var parsedMinutes) && parsedMinutes > 0
            ? parsedMinutes
            : 60;

        // 2. DI 컨테이너에 계층별 객체 등록 (Data -> Services -> ViewModels/Views)
        var services = new ServiceCollection();
        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connStr));

        // Repositories
        services.AddTransient<UserRepository>();
        services.AddTransient<MasterRepository>();
        services.AddTransient<PatientRepository>();
        services.AddTransient<VisitRepository>();
        services.AddTransient<EmrRepository>();
        services.AddTransient<BillingRepository>();
        services.AddTransient<AuditLogRepository>();

        // Services
        services.AddSingleton(_ => new SessionContext { SessionTimeoutMinutes = sessionTimeoutMinutes });
        services.AddTransient<AuditService>();
        services.AddTransient<AuthService>();
        services.AddTransient<ReceptionService>();
        services.AddTransient<EmrService>();
        services.AddTransient<BillingService>();

        // ViewModels & Views
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<ReceptionViewModel>();
        services.AddTransient<EmrViewModel>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<PaymentHistoryViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        ShowLoginAndMainLoop();
    }

    private void ShowLoginAndMainLoop()
    {
        if (_serviceProvider is null) return;

        while (true)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.ShowDialog();

            if (!mainWindow.IsLoggedOut)
            {
                Shutdown();
                return;
            }
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = FindSqlException(e.Exception) is { } sqlEx
            ? $"데이터베이스에 연결할 수 없습니다.\nSQL Server 서비스 실행 여부와 appsettings.json 설정을 확인하세요.\n\n상세: {sqlEx.Message}"
            : $"오류가 발생했습니다.\n\n{e.Exception.Message}";

        MessageBox.Show(message, "연결 및 실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static SqlException? FindSqlException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is SqlException sqlEx) return sqlEx;
            ex = ex.InnerException;
        }
        return null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
