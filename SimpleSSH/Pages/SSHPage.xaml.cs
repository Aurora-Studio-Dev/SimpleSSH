using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SimpleSSH.Helper;
using SimpleSSH.Services;

namespace SimpleSSH.Pages;

public partial class SSHPage
{
    private DispatcherTimer? _inactivityTimer;

    private DateTime _lastActivityTime = DateTime.Now;
    private ServerInfoConfigHelper.ServerInfo? _selectedServer;
    private SshConnectionService? _sshConnectionService;

    public SSHPage()
    {
        InitializeComponent();
        LoadServers();
        InitializeInactivityTimer();

        SizeChanged += SSHPage_SizeChanged;
    }

    public bool IsConnecting { get; private set; }

    private void SSHPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateLayoutForWindowSize(e.NewSize);
    }

    private void UpdateLayoutForWindowSize(Size newSize)
    {
        if (newSize.Width < 900)
            LeftColumn.Width = new GridLength(250);
        else
            LeftColumn.Width = new GridLength(300);
    }

    private void LoadServers()
    {
        ServerComboBox.ItemsSource = ServerInfoConfigHelper.CurrentConfig.Servers;
        if (ServerInfoConfigHelper.CurrentConfig.Servers.Count > 0) ServerComboBox.SelectedIndex = 0;
    }

    private void InitializeInactivityTimer()
    {
        _inactivityTimer = new DispatcherTimer();
        _inactivityTimer.Interval = TimeSpan.FromMinutes(1);
        _inactivityTimer.Tick += CheckInactivity;
        _inactivityTimer.Start();
    }

    private void CheckInactivity(object? sender, EventArgs e)
    {
        if ((DateTime.Now - _lastActivityTime).TotalMinutes >= 5)
            Disconnect(false);
    }

    private void UpdateActivityTime()
    {
        _lastActivityTime = DateTime.Now;
    }

    private void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServerComboBox.SelectedItem is ServerInfoConfigHelper.ServerInfo selectedServer)
        {
            _selectedServer = selectedServer;
            ServerInfo.Text = "用户: "+selectedServer.ServerUsername + "\nIP: " + selectedServer.ServerIp;
            PasswordBox.Password = string.Empty;
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateActivityTime();

        if (_selectedServer == null)
        {
            MessageBox.Show("请选择一个服务器。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("请输入密码。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_sshConnectionService != null) Disconnect(false);

        IsConnecting = true;
        ConnectButton.IsEnabled = false;
        ConnectionProgressBar.Visibility = Visibility.Visible;
        ConnectionProgressBar.IsIndeterminate = true;
        ConnectionStatusText.Visibility = Visibility.Visible;
        ServerComboBox.IsEnabled = false;
        PasswordBox.IsEnabled = false;

        try
        {
            TerminalTextBox.Text = "";
            
            _sshConnectionService = new SshConnectionService();
            _sshConnectionService.OutputReceived += OnSshOutputReceived;
            _sshConnectionService.ErrorReceived += OnSshErrorReceived;
            _sshConnectionService.ConnectionStatusChanged += OnSshConnectionStatusChanged;

            var isConnected = await _sshConnectionService.ConnectAsync(_selectedServer, password);

            if (isConnected)
            {
                AppendToTerminal($"用户{_selectedServer.ServerUsername}于{DateTime.Now:yyyy-MM-dd HH:mm:ss}连接到 {_selectedServer.ServerName} ({_selectedServer.ServerIp})的操作成功！\n");
                
                CommandTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;

                DisconnectButton.Visibility = Visibility.Visible;

                CommandTextBox.Focus();
            }
            else
            {
                AppendToTerminal("连接失败！\n");
            }
        }
        catch (Exception ex)
        {
            AppendToTerminal($"连接失败: {ex.Message}\n");
        }
        finally
        {
            IsConnecting = false;
            ConnectButton.IsEnabled = true;
            ConnectionProgressBar.Visibility = Visibility.Collapsed;
            ConnectionStatusText.Visibility = Visibility.Collapsed;
            ServerComboBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;
        }
    }

    private void OnSshOutputReceived(object? sender, string e)
    {
        AppendToTerminal(e);
    }

    private void OnSshErrorReceived(object? sender, string e)
    {
        AppendToTerminal($"错误: {e}");
    }

    private void OnSshConnectionStatusChanged(object? sender, EventArgs e)
    {
        if (_sshConnectionService != null && !_sshConnectionService.IsConnected)
            Dispatcher.Invoke(() =>
            {
                CommandTextBox.IsEnabled = false;
                SendButton.IsEnabled = false;
                DisconnectButton.Visibility = Visibility.Collapsed;
            });
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        Disconnect(true);
    }

    private void Disconnect(bool clearPassword)
    {
        if (_sshConnectionService != null)
        {
            _sshConnectionService.OutputReceived -= OnSshOutputReceived;
            _sshConnectionService.ErrorReceived -= OnSshErrorReceived;
            _sshConnectionService.ConnectionStatusChanged -= OnSshConnectionStatusChanged;
            _sshConnectionService.Dispose();
            _sshConnectionService = null;
        }

        CommandTextBox.IsEnabled = false;
        SendButton.IsEnabled = false;

        DisconnectButton.Visibility = Visibility.Collapsed;

        TerminalTextBox.Text = "";

        if (clearPassword) PasswordBox.Password = string.Empty;
    }

    private void AppendToTerminal(string text)
    {
        Dispatcher.Invoke(() =>
        {
            TerminalTextBox.AppendText(text);
            TerminalTextBox.ScrollToEnd();
        });
    }

    private void CommandTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        UpdateActivityTime();

        if (e.Key == Key.Enter) ExecuteCommand();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateActivityTime();
        ExecuteCommand();
    }

    private async void ExecuteCommand()
    {
        if (string.IsNullOrWhiteSpace(CommandTextBox.Text) || _sshConnectionService == null ||
            !_sshConnectionService.IsConnected)
            return;

        var commandText = CommandTextBox.Text;

        try
        {
            await _sshConnectionService.ExecuteCommandAsync(commandText);

            CommandTextBox.Text = string.Empty;
        }
        catch (Exception ex)
        {
            AppendToTerminal($"发送命令时出错: {ex.Message}\n");
        }
    }
    
}