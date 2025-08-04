using System.Windows;
using SimpleSSH.Helper;

namespace SimpleSSH.Windows;

public partial class EditServerInfo : Window
{
    private readonly bool _isEditMode;
    private readonly ServerInfoConfigHelper.ServerInfo _serverInfo;

    // 添加模式构造函数
    public EditServerInfo()
    {
        InitializeComponent();
        _isEditMode = false;
        Title = "添加服务器";
    }

    // 编辑模式构造函数
    public EditServerInfo(ServerInfoConfigHelper.ServerInfo serverInfo)
    {
        InitializeComponent();
        _serverInfo = serverInfo;
        _isEditMode = true;
        Title = "编辑服务器";

        // 填充现有数据
        ServerName.Text = _serverInfo.ServerName;
        ServerIp.Text = _serverInfo.ServerIp;
        ServerUsername.Text = _serverInfo.ServerUsername;
        ServerPort.Text = _serverInfo.ServerPort.ToString();
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

        if (_isEditMode)
        {
            // 编辑模式下更新现有服务器信息
            _serverInfo.ServerName = ServerName.Text;
            _serverInfo.ServerIp = ServerIp.Text;
            _serverInfo.ServerUsername = ServerUsername.Text;
            _serverInfo.ServerPort = port;
        }
        else
        {
            // 添加模式下创建新服务器信息
            var newServer = new ServerInfoConfigHelper.ServerInfo
            {
                ServerName = ServerName.Text,
                ServerIp = ServerIp.Text,
                ServerUsername = ServerUsername.Text,
                ServerPort = port
            };

            ServerInfoConfigHelper.CurrentConfig.Servers.Add(newServer);
        }

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