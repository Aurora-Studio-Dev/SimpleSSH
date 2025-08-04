using System.Windows;
using SimpleSSH.Helper;

namespace SimpleSSH.Windows;

public partial class AddServerWindow : Window
{
    public AddServerWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ServerName.Text) ||
            string.IsNullOrWhiteSpace(ServerIp.Text) ||
            string.IsNullOrWhiteSpace(ServerUsername.Text) ||
            string.IsNullOrWhiteSpace(ServerPort.Text))
        {
            MessageBox.Show("请填写所有字段。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 验证端口
        if (!int.TryParse(ServerPort.Text, out var port) || port < 1 || port > 65535)
        {
            MessageBox.Show("请输入有效的端口号（1-65535）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newServer = new ServerInfoConfigHelper.ServerInfo
        {
            ServerName = ServerName.Text,
            ServerUsername = ServerUsername.Text,
            ServerIp = ServerIp.Text,
            ServerPort = port
        };

        ServerInfoConfigHelper.CurrentConfig.Servers.Add(newServer);
        ServerInfoConfigHelper.SaveConfig();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}