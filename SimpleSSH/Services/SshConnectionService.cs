using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using SimpleSSH.Helper;

namespace SimpleSSH.Services;

public class SshConnectionService : IDisposable
{
    private string _currentPath = "~";
    private ServerInfoConfigHelper.ServerInfo _serverInfo;
    private ShellStream _shellStream;
    private SshClient _sshClient;

    public bool IsConnected => _sshClient?.IsConnected ?? false;

    public void Dispose()
    {
        Disconnect();
    }

    public async Task<bool> ConnectAsync(ServerInfoConfigHelper.ServerInfo serverInfo, string password)
    {
        try
        {
            _serverInfo = serverInfo;
            _sshClient = new SshClient(serverInfo.ServerIp, serverInfo.ServerPort, serverInfo.ServerUsername, password);

            await Task.Run(() => _sshClient.Connect());

            if (_sshClient.IsConnected)
            {
                // 使用CreateShellStream替代旧版本的CreateShell
                _shellStream = _sshClient.CreateShellStream("xterm", 80, 24, 0, 0, 1024);

                // 订阅数据接收事件
                _shellStream.DataReceived += OnDataReceived;

                // 等待shell初始化完成
                await Task.Delay(100);

                ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
        catch
        {
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            _shellStream?.Dispose();
            _sshClient?.Dispose();
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // 忽略断开连接时的异常
        }
    }

    public async Task ExecuteCommandAsync(string command)
    {
        if (_shellStream == null)
            throw new InvalidOperationException("Shell stream is not initialized");

        await Task.Run(() => { _shellStream.WriteLine(command); });
    }

    public async Task ChangeDirectoryAsync(string path)
    {
        if (_shellStream == null)
            throw new InvalidOperationException("Shell stream is not initialized");

        await Task.Run(() => { _shellStream.WriteLine($"cd {path}"); });

        // 给服务器一些时间处理cd命令
        await Task.Delay(100);
    }

    public string GetCurrentPath()
    {
        return _currentPath;
    }

    private async Task UpdateCurrentPathAsync()
    {
        if (!IsConnected) return;

        try
        {
            _currentPath = await Task.Run(() =>
            {
                using var command = _sshClient.CreateCommand("pwd");
                command.Execute();
                return command.Result?.Trim() ?? "/";
            });
        }
        catch
        {
            _currentPath = "/";
        }
    }

    private void OnDataReceived(object sender, ShellDataEventArgs e)
    {
        if (e.Data != null && e.Data.Length > 0)
        {
            var data = Encoding.UTF8.GetString(e.Data);
            OutputReceived?.Invoke(this, data);
        }
    }

    public event EventHandler<string> OutputReceived;
    public event EventHandler<string> ErrorReceived;
    public event EventHandler ConnectionStatusChanged;
}