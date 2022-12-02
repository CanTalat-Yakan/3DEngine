using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Editor.Controller
{
    internal class TabViewItemDataTemplate
    {
        public string Header;
        public object Content;
        public Symbol Symbol;
    }

    internal class TabViewPageController
    {
        public TabView TabView;

        public TabViewPageController(params TabViewItemDataTemplate[] icollection)
        {
            TabView = new TabView()
            {
                TabWidthMode = TabViewWidthMode.SizeToContent,
                CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto,
                IsAddTabButtonVisible = true,
                Padding = new Thickness(8, 8, 8, 0)
            };
            TabView.AddTabButtonClick += TabView_AddButtonClick;
            //m_TabView.TabCloseRequested += TabView_TabCloseRequested;

            foreach (var item in icollection)
                TabView.TabItems.Add(CreateNewTab(item));
        }

        public async void TabViewWindowingButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Window.Current.Content = new Page();
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        private void TabView_AddButtonClick(TabView sender, object args)
        {
            var item = new TabViewItemDataTemplate() { Header = "New Tab", Content = new Page(), Symbol = Symbol.View };
            TabView.TabItems.Add(CreateNewTab(item));
        }

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private TabViewItem CreateNewTab(TabViewItemDataTemplate i)
        {
            TabViewItem newItem = new TabViewItem
            {
                Header = i.Header + "⠀⠀⠀⠀⠀⠀⠀⠀",
                Content = i.Content,
                IconSource = new SymbolIconSource() { Symbol = i.Symbol },
                IsClosable = false
            };

            return newItem;
        }
    }
}
