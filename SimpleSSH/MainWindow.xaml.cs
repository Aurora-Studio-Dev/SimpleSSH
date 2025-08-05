using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Media.Animation;
using SimpleSSH.Helper;
using SimpleSSH.Pages;
using Page = System.Windows.Controls.Page;

namespace SimpleSSH;

public partial class MainWindow
{
    public static string AppVersion { get; set; } = "dev v0.2";
    
    private readonly Page _home = new HomePage();
    private readonly Page _settings = new SettingsPage();
    private readonly Page _ssh = new SshPage();
    private readonly Page _about = new AboutPage();

    private readonly List<(string Title, string Content, Type PageType)> _searchableContent = new();

    public MainWindow()
    {
        InitializeComponent();
        DefaultDataLoad();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UsingConfig();
        Loaded += MainWindow_Loaded;
        InitializeSearchableContent();
        TitleBarSearchBox.TextChanged += TitleBarSearchBox_TextChanged;
        TitleBarSearchBox.QuerySubmitted += TitleBarSearchBox_QuerySubmitted;
    }
    private void InitializeSearchableContent()
    {
        _searchableContent.Add(("首页", "首页 服务器列表 添加 编辑 删除 刷新", typeof(HomePage)));
        _searchableContent.Add(("SSH", "SSH连接 连接设置 服务器 密码 连接 断开", typeof(SshPage)));
        _searchableContent.Add(("设置", "设置 个性化 主题 浅色 深色", typeof(SettingsPage)));
        _searchableContent.Add(("关于", "关于 SimpleSSH GitHub Bilibili QQ群 Facebook Youtube 检查更新", typeof(AboutPage)));
    }

    private void TitleBarSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text.ToLower();
            if (string.IsNullOrWhiteSpace(query))
            {
                sender.ItemsSource = new List<string>();
                return;
            }

            var suggestions = _searchableContent
                .Where(item => item.Title.ToLower().Contains(query) || item.Content.ToLower().Contains(query))
                .Select(item => item.Title)
                .ToList();

            sender.ItemsSource = suggestions;
        }
    }

    private void TitleBarSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText.ToLower();
        var selected = _searchableContent
            .FirstOrDefault(item => item.Title.Equals(args.ChosenSuggestion?.ToString(), StringComparison.OrdinalIgnoreCase) ||
                              item.Title.ToLower().Contains(query) ||
                              item.Content.ToLower().Contains(query));

        if (selected.PageType != null)
        {
            NavigateTo(selected.PageType, new EntranceNavigationTransitionInfo());
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarSearchBox.Focusable = true;
        TitleBarSearchBox.IsHitTestVisible = true;
    }

    private void UsingConfig()
    {
        try
        {
            var theme = SettingsConfigHelper.CurrentConfig.AppTheme;
            if (theme == "dark") ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            else if (theme == "light") ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }
        catch
        {
            return;
        }
    }

    private void DefaultDataLoad()
    {
        CurrentPage.Content = _home;
    }

    private void NavigationTriggered(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateTo(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
        }
        else if (args.InvokedItemContainer?.Tag != null)
        {
            var tag = args.InvokedItemContainer.Tag.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                var pageType = Type.GetType(tag);
                if (pageType != null) NavigateTo(pageType, args.RecommendedNavigationTransitionInfo);
            }
        }
    }

    private void NavigateTo(Type navPageType, NavigationTransitionInfo transitionInfo)
    {
        var preNavPageType = CurrentPage.Content?.GetType();
        if (navPageType == preNavPageType) return;

        var pageMapping = new Dictionary<Type, Page>
        {
            { typeof(HomePage), _home },
            { typeof(SshPage), _ssh },
            { typeof(SettingsPage), _settings },
            { typeof(AboutPage), _about }
        };

        Page targetPage;
        if (pageMapping.TryGetValue(navPageType, out var predefinedPage))
        {
            targetPage = predefinedPage;
        }
        else
        {
            targetPage = (Page)Activator.CreateInstance(navPageType);
        }
        
        CurrentPage.Navigate(targetPage, transitionInfo);
        UpdateNavigationViewSelection(navPageType);
    }
    
    private void UpdateNavigationViewSelection(Type navPageType)
    {
        foreach (var item in NavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem)
            {
                if (navItem.Tag?.ToString() == $"SimpleSSH.Pages.{navPageType.Name}")
                {
                    NavigationView.SelectedItem = navItem;
                    break;
                }
            }
        }
    }
}
