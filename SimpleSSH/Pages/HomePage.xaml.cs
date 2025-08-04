using System.ComponentModel;
using System.Security.Principal;
using System.Windows;
using SimpleSSH.Helper;
using SimpleSSH.Windows;

namespace SimpleSSH.Pages;

public partial class HomePage
{
    public HomePage()
    {
        InitializeComponent();
        SetLittleTips();
        LoadServerList();
    }

    private void SetLittleTips()
    {
        var systemUsername = WindowsIdentity.GetCurrent().Name;
        var username = systemUsername.Substring(systemUsername.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        var systemTime = DateTime.Now.TimeOfDay;
        if (systemTime >= new TimeSpan(6, 0, 0) && systemTime <= new TimeSpan(12, 0, 0))
            LittleTips.Text = $"\ud83d\ude2a早上好呀，亲爱的{username}！您吉祥！";
        else if (systemTime >= new TimeSpan(12, 0, 0) && systemTime <= new TimeSpan(18, 0, 0))
            LittleTips.Text = $"\u2600下午好呀，亲爱的{username}！吃了吗您？";
        else if (systemTime >= new TimeSpan(18, 0, 0) && systemTime <= new TimeSpan(24, 0, 0))
            LittleTips.Text = $"\ud83c\udf1cAUV，晚上好呀，亲爱的{username}！";
        else
            LittleTips.Text = "\ud83d\udecc\ud83c\udffc好家伙，这都几点了你居然还在工作？赶紧去睡觉";
    }

    private void LoadServerList()
    {
        var selectableServers = ServerInfoConfigHelper.CurrentConfig.Servers.Select(s => new SelectableServerInfo(s))
            .ToList();
        ServerListView.ItemsSource = selectableServers;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var addServerWindow = new AddServerWindow();
        addServerWindow.Owner = Window.GetWindow(this);
        addServerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (addServerWindow.ShowDialog() == true) LoadServerList();
    }

    private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        var servers = ServerListView.ItemsSource as List<SelectableServerInfo>;
        if (servers != null)
            foreach (var server in servers)
                server.IsSelected = true;
    }

    private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        var servers = ServerListView.ItemsSource as List<SelectableServerInfo>;
        if (servers != null)
            foreach (var server in servers)
                server.IsSelected = false;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var servers = ServerListView.ItemsSource as List<SelectableServerInfo>;
        if (servers != null)
        {
            var selectedServers = servers.Where(s => s.IsSelected).ToList();
            if (selectedServers.Count > 0)
            {
                var result = MessageBox.Show($"确定要删除选中的 {selectedServers.Count} 个服务器吗？", "确认删除", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var server in selectedServers)
                        ServerInfoConfigHelper.CurrentConfig.Servers.Remove(server.ServerInfo);
                    ServerInfoConfigHelper.SaveConfig();
                    LoadServerList(); // 重新加载列表
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的服务器。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var servers = ServerListView.ItemsSource as List<SelectableServerInfo>;
        if (servers != null)
        {
            var selectedServers = servers.Where(s => s.IsSelected).ToList();
            if (selectedServers.Count > 1)
            {
                MessageBox.Show("请选择一个服务器进行编辑。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedServers.Count == 1)
            {
                var editWindow = new EditServerInfo(selectedServers[0].ServerInfo);
                editWindow.Owner = Window.GetWindow(this);
                editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (editWindow.ShowDialog() == true) LoadServerList(); // 重新加载列表以反映更改
            }
            else
            {
                MessageBox.Show("请选择一个要编辑的服务器。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadServerList();
    }
}

public class SelectableServerInfo : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableServerInfo(ServerInfoConfigHelper.ServerInfo serverInfo)
    {
        ServerInfo = serverInfo;
    }

    public ServerInfoConfigHelper.ServerInfo ServerInfo { get; }

    public string ServerName => ServerInfo.ServerName;
    public string ServerIp => ServerInfo.ServerIp;
    public string ServerUsername => ServerInfo.ServerUsername;
    public int ServerPort => ServerInfo.ServerPort;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}