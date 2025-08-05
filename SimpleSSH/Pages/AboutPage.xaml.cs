using System.Diagnostics;
using System.Windows;
using SimpleSSH.Windows;

namespace SimpleSSH.Pages;

public partial class AboutPage
{
    public AboutPage()
    {
        InitializeComponent();
        ThisVersion.Text = $"当前 SimpleSSH 版本：{MainWindow.AppVersion}";
    }
// ... existing code ...
private void Gh_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/Aurora-Studio-Dev/SimpleSSH") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Bl_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://space.bilibili.com/1910324323") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Qq_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://qm.qq.com/cgi-bin/qm/qr?k=jISKl0XHfq8AnT77pqI98L1nZrQLdiUp&jump_from=webapi&authKey=fpVuY0SA2OJQGezSr8AP4bVw6QXuhzf1uT4ELDr6teRoEFuZBtXbxux83gCIBdSS") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Fb_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.facebook.com/profile.php?id=61559123580368") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Yt_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.youtube.com/@thz-aurora") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
// ... existing code ...


    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var updateWindow = new UpdateWindow();
        updateWindow.Owner = Window.GetWindow(this);
        updateWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        updateWindow.ShowDialog();
    }
}