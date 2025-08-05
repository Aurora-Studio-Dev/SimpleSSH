using System.Windows;

namespace SimpleSSH.Windows;

public partial class UpdateWindow
{
    public UpdateWindow()
    {
        InitializeComponent();
        GetVersion();
    }

    private void GetVersion()
    {
        var appVersion = MainWindow.AppVersion;
        ThisVersion.Text = $"当前 SimpleSSH 版本：{appVersion}";
        
        
        
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void Update_Click(object sender, RoutedEventArgs e)
    {
        
    }
}