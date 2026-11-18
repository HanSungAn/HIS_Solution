using System.Windows;

namespace HIS.WPF.Views;

public partial class ExcelExportReasonDialog : Window
{
    public string DownloadReason { get; private set; } = string.Empty;

    public ExcelExportReasonDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => ReasonBox.Focus();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var reason = ReasonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            WarningText.Text = "다운로드 사유를 반드시 입력해야 합니다.";
            ReasonBox.Focus();
            return;
        }

        DownloadReason = reason;
        DialogResult = true;
        Close();
    }
}
