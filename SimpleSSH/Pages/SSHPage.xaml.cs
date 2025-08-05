using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SimpleSSH.Helper;
using SimpleSSH.Services;

namespace SimpleSSH.Pages;

public partial class SshPage
{
    private readonly Dictionary<TabItem, SshConnectionService> _sshConnections = new();
    private DispatcherTimer? _inactivityTimer;

    private DateTime _lastActivityTime = DateTime.Now;
    private ServerInfoConfigHelper.ServerInfo? _selectedServer;

    public SshPage()
    {
        InitializeComponent();
        LoadServers();
        InitializeInactivityTimer();

        SizeChanged += SshPage_SizeChanged;
    }

    public bool IsConnecting { get; private set; }

    private void SshPage_SizeChanged(object sender, SizeChangedEventArgs e)
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
            ServerInfo.Text = "用户: " + selectedServer.ServerUsername + "\nIP: " + selectedServer.ServerIp;
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

        // 创建新的标签页用于这个连接
        var newTab = new TabItem();
        var terminalTextBox = new TextBox
        {
            Background = Brushes.Black,
            Foreground = Brushes.LightGray,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Padding = new Thickness(10)
        };

        var commandPanel = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
        var commandTextBox = new TextBox
        {
            Height = 35,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = false
        };
        var sendButton = new Button
        {
            Content = "发送",
            Height = 35,
            MinWidth = 80,
            Margin = new Thickness(10, 0, 0, 0),
            IsEnabled = false
        };

        DockPanel.SetDock(sendButton, Dock.Right);
        commandPanel.Children.Add(sendButton);
        commandPanel.Children.Add(commandTextBox);

        var dockPanel = new DockPanel();
        DockPanel.SetDock(commandPanel, Dock.Bottom);
        dockPanel.Children.Add(commandPanel);
        dockPanel.Children.Add(terminalTextBox);

        newTab.Content = dockPanel;
        newTab.Header = _selectedServer.ServerName;

        commandTextBox.KeyDown += (s, args) =>
        {
            UpdateActivityTime();
            if (args.Key == Key.Enter) ExecuteCommandInTab(newTab, commandTextBox, terminalTextBox);
        };

        sendButton.Click += (s, args) =>
        {
            UpdateActivityTime();
            ExecuteCommandInTab(newTab, commandTextBox, terminalTextBox);
        };

        TerminalTabControl.Items.Add(newTab);
        TerminalTabControl.SelectedItem = newTab;

        IsConnecting = true;
        ConnectButton.IsEnabled = false;
        ConnectionProgressBar.Visibility = Visibility.Visible;
        ConnectionProgressBar.IsIndeterminate = true;
        ConnectionStatusText.Visibility = Visibility.Visible;
        ServerComboBox.IsEnabled = false;
        PasswordBox.IsEnabled = false;

        try
        {
            terminalTextBox.Text = "";

            var sshConnectionService = new SshConnectionService();
            _sshConnections[newTab] = sshConnectionService;

            sshConnectionService.OutputReceived += (s, output) => { AppendToTerminalInTab(terminalTextBox, output); };
            sshConnectionService.ErrorReceived += (s, error) =>
            {
                AppendToTerminalInTab(terminalTextBox, $"错误: {error}");
            };
            sshConnectionService.ConnectionStatusChanged += (s, args) =>
            {
                if (!_sshConnections.ContainsKey(newTab) || !_sshConnections[newTab].IsConnected)
                    Dispatcher.Invoke(() =>
                    {
                        commandTextBox.IsEnabled = false;
                        sendButton.IsEnabled = false;
                    });
            };

            var isConnected = await sshConnectionService.ConnectAsync(_selectedServer, password);

            if (isConnected)
            {
                commandTextBox.IsEnabled = true;
                sendButton.IsEnabled = true;

                commandTextBox.Focus();
            }
            else
            {
                AppendToTerminalInTab(terminalTextBox, "连接失败！\n");
            }
        }
        catch (Exception ex)
        {
            AppendToTerminalInTab(new TextBox(), $"连接失败: {ex.Message}\n");
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


    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (TerminalTabControl.SelectedItem is TabItem selectedTab && _sshConnections.ContainsKey(selectedTab))
        {
            var connection = _sshConnections[selectedTab];
            connection.Dispose();
            _sshConnections.Remove(selectedTab);
            TerminalTabControl.Items.Remove(selectedTab);
        }
    }

    private void Disconnect(bool clearPassword)
    {
        foreach (var connection in _sshConnections.Values) connection.Dispose();
        _sshConnections.Clear();

        TerminalTabControl.Items.Clear();
        TerminalTabControl.Items.Add(CreateWelcomeTab());

        CommandTextBox.IsEnabled = false;
        SendButton.IsEnabled = false;

        DisconnectButton.Visibility = Visibility.Collapsed;

        TerminalTextBox.Text = "";

        if (clearPassword) PasswordBox.Password = string.Empty;
    }

    private TabItem CreateWelcomeTab()
    {
        var welcomeTab = new TabItem();
        welcomeTab.Header = "欢迎";

        var grid = new Grid();
        var row1 = new RowDefinition { Height = GridLength.Auto };
        var row2 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
        grid.RowDefinitions.Add(row1);
        grid.RowDefinitions.Add(row2);

        var contentTextBlock = new TextBlock
        {
            Text = "请选择一个服务器并点击连接按钮开始SSH会话\n您可以在上方的标签页切换会话进程，点击x关闭进程",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)FindResource("TextFillColorSecondaryBrushKey")
        };
        Grid.SetRow(contentTextBlock, 0);
        Grid.SetRowSpan(contentTextBlock, 2);
        grid.Children.Add(contentTextBlock);

        welcomeTab.Content = grid;
        return welcomeTab;
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
        if (string.IsNullOrWhiteSpace(CommandTextBox.Text))
            return;

        // 执行命令的逻辑需要修改以适应当前选中的标签页
        // 这里暂时保留原逻辑作为示例
        /*
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
        */
    }

    // 为特定标签页执行命令
    private async void ExecuteCommandInTab(TabItem tabItem, TextBox commandTextBox, TextBox terminalTextBox)
    {
        if (string.IsNullOrWhiteSpace(commandTextBox.Text) || !_sshConnections.ContainsKey(tabItem) ||
            !_sshConnections[tabItem].IsConnected)
            return;

        var commandText = commandTextBox.Text;

        try
        {
            await _sshConnections[tabItem].ExecuteCommandAsync(commandText);
            commandTextBox.Text = string.Empty;
        }
        catch (Exception ex)
        {
            AppendToTerminalInTab(terminalTextBox, $"发送命令时出错: {ex.Message}\n");
        }
    }

    // 向特定标签页的终端添加文本
    private void AppendToTerminalInTab(TextBox terminalTextBox, string text)
    {
        Dispatcher.Invoke(() =>
        {
            terminalTextBox.AppendText(text);
            terminalTextBox.ScrollToEnd();
        });
    }

    // 添加服务器管理功能
    private void AddServerToManagement_Click(object sender, RoutedEventArgs e)
    {
        // 如果当前有选中的服务器且已连接，则添加到管理列表
        if (_selectedServer != null)
        {
            // 检查是否已连接到服务器
            var isConnected = false;
            foreach (var connection in _sshConnections.Values)
                if (connection.IsConnected)
                {
                    isConnected = true;
                    break;
                }

            // 创建管理窗口
            var managementWindow = new Window
            {
                Title = "服务器管理",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                Text = $"服务器名称: {_selectedServer.ServerName}\n" +
                       $"服务器IP: {_selectedServer.ServerIp}\n" +
                       $"用户名: {_selectedServer.ServerUsername}\n" +
                       $"连接状态: {(isConnected ? "已连接" : "未连接")}",
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 添加到管理列表按钮
            var addToManagementButton = new Button
            {
                Content = "添加到管理列表",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 创建列表显示已管理的服务器
            var managedServersList = new ListBox
            {
                Height = 100,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 将当前服务器添加到列表中
            managedServersList.Items.Add($"{_selectedServer.ServerName} ({_selectedServer.ServerIp})");

            var closeButton = new Button
            {
                Content = "关闭",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(10, 5, 10, 5)
            };

            addToManagementButton.Click += (s, args) =>
            {
                var serverInfo = $"{_selectedServer.ServerName} ({_selectedServer.ServerIp})";
                if (!managedServersList.Items.Contains(serverInfo))
                {
                    managedServersList.Items.Add(serverInfo);
                    MessageBox.Show("服务器已添加到管理列表中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("服务器已在管理列表中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            closeButton.Click += (s, args) => managementWindow.Close();

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(addToManagementButton);
            stackPanel.Children.Add(new TextBlock { Text = "已管理的服务器:", Margin = new Thickness(0, 10, 0, 5) });
            stackPanel.Children.Add(managedServersList);
            stackPanel.Children.Add(closeButton);

            managementWindow.Content = stackPanel;
            managementWindow.ShowDialog();
        }
        else
        {
            MessageBox.Show("请选择一个服务器再进行管理操作。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}