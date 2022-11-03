using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Editor.Controls
{
    internal class TabViewItemDataTemplate
    {
        public string Header;
        public object Content;
        public Symbol Symbol;
    }

    internal class TabViewPageController
    {
        internal TabView _tabView;

        public TabViewPageController(params TabViewItemDataTemplate[] icollection)
        {
            _tabView = new TabView() { TabWidthMode = TabViewWidthMode.Equal, CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto, IsAddTabButtonVisible = false };
            _tabView.AddTabButtonClick += TabView_AddButtonClick;
            //m_TabView.TabCloseRequested += TabView_TabCloseRequested;

            foreach (var item in icollection)
                _tabView.TabItems.Add(CreateNewTab(item));
        }

        void TabView_AddButtonClick(TabView sender, object args)
        {
            var item = new TabViewItemDataTemplate() { Header = "Viewport", Content = new Page(), Symbol = Symbol.View };
            _tabView.TabItems.Add(CreateNewTab(item));
        }

        void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        TabViewItem CreateNewTab(TabViewItemDataTemplate i)
        {
            TabViewItem newItem = new TabViewItem
            {
                Header = i.Header,
                Content = i.Content,
                IconSource = new SymbolIconSource() { Symbol = i.Symbol },
                IsClosable = false
            };

            return newItem;
        }

        internal async void TabViewWindowingButton_Click(object sender, RoutedEventArgs e)
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
    }
}
