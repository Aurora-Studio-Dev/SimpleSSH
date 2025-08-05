using System.Text;
using iNKORE.UI.WPF.Modern.Controls;
using Renci.SshNet;
using Renci.SshNet.Common;
using SimpleSSH.Helper;

namespace SimpleSSH.Services;

public class SshConnectionService : IDisposable
{
    private ServerInfoConfigHelper.ServerInfo? _serverInfo;
    private ShellStream? _shellStream;
    private SshClient? _sshClient;

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

            await Task.Run(() => _sshClient!.Connect());

            if (_sshClient!.IsConnected)
            {
                _shellStream = _sshClient!.CreateShellStream("xterm", 80, 24, 0, 0, 1024);

                _shellStream!.DataReceived += OnDataReceived;
                await Task.Delay(100);

                ConnectionStatusChanged.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
        catch (SshAuthenticationException ex)
        {
            try
            {
                ErrorReceived.Invoke(this, $"身份认证失败: {ex.Message}");
                MessageBox.Show("身份认证失败: " + ex.Message, "SSH会话服务");
            }
            catch
            {
                return false;
            }
            finally
            {
                Disconnect();
            }

            return false;
        }
        catch (Exception ex)
        {
            try
            {
                ErrorReceived.Invoke(this, $"连接失败: {ex.Message}");
                MessageBox.Show("远程连接失败: " + ex.Message, "SSH会话服务");
            }
            catch
            {
                // 忽略
            }
            finally
            {
                Disconnect();
            }

            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            _shellStream?.Dispose();
            _sshClient?.Dispose();
            ConnectionStatusChanged.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            //
        }
    }

    public async Task ExecuteCommandAsync(string command)
    {
        if (_shellStream == null)
            throw new InvalidOperationException("Shell stream is not initialized");

        await Task.Run(() => { _shellStream!.WriteLine(command); }); // 断言 _shellStream 不为 null
    }

    private void OnDataReceived(object? sender, ShellDataEventArgs e)
    {
        if (e.Data != null && e.Data.Length > 0)
        {
            var data = Encoding.UTF8.GetString(e.Data);
            OutputReceived.Invoke(this, data);
        }
    }

    public event EventHandler<string> OutputReceived;
    public event EventHandler<string> ErrorReceived;
    public event EventHandler ConnectionStatusChanged;
}