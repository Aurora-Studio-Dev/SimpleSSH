using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using SimpleSSH.Pages;
using Page = System.Windows.Controls.Page;

namespace SimpleSSH;

public partial class MainWindow
{
    private readonly Page _home = new HomePage();
    private readonly Page _settings = new SettingsPage();
    private readonly Page _ssh = new SSHPage();

    public MainWindow()
    {
        InitializeComponent();
        DefaultDataLound();
    }

    private void DefaultDataLound()
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
            var pageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
            if (pageType != null) NavigateTo(pageType, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavigateTo(Type navPageType, NavigationTransitionInfo transitionInfo)
    {
        if (CurrentPage?.Content == null || navPageType == null) return;

        var preNavPageType = CurrentPage.Content.GetType();
        if (navPageType == preNavPageType) return;

        var pageMapping = new Dictionary<Type, Page>
        {
            { typeof(HomePage), _home },
            { typeof(SSHPage), _ssh },
            { typeof(SettingsPage), _settings }
        };

        if (pageMapping.TryGetValue(navPageType, out var targetPage)) CurrentPage.Navigate(targetPage, transitionInfo);
    }
}