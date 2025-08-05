using System.Windows.Controls;
using iNKORE.UI.WPF.Modern;
using SimpleSSH.Helper;

namespace SimpleSSH.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        SetTheme();
        AppTheme.SelectionChanged += AppTheme_SelectionChanged;
    }

    private void SetTheme()
    {
        try
        {
            var theme = SettingsConfigHelper.CurrentConfig.AppTheme;
            if (theme == "dark") AppTheme.SelectedIndex = 1;
            else if (theme == "light") AppTheme.SelectedIndex = 0;
            else AppTheme.SelectedIndex = 0;
        }
        catch
        {
            return;
        }
    }

    private void AppTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (AppTheme.SelectedIndex == 0)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                SettingsConfigHelper.CurrentConfig.AppTheme = "light";
                SettingsConfigHelper.SaveConfig();
            }
            else if (AppTheme.SelectedIndex == 1)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                SettingsConfigHelper.CurrentConfig.AppTheme = "dark";
                SettingsConfigHelper.SaveConfig();
            }
        }
        catch
        {
            return;
        }
    }
}