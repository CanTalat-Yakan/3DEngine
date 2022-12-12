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

    internal class TabViewPage
    {
        public TabView TabView;

        public TabViewPage(params TabViewItemDataTemplate[] tabViewDataTemplate)
        {
            TabView = new()
            {
                TabWidthMode = TabViewWidthMode.SizeToContent,
                CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto,
                IsAddTabButtonVisible = true,
                Padding = new(8, 8, 8, 0)
            };
            TabView.AddTabButtonClick += TabView_AddButtonClick;
            //m_TabView.TabCloseRequested += TabView_TabCloseRequested;

            foreach (var dataTemplate in tabViewDataTemplate)
                TabView.TabItems.Add(CreateNewTab(dataTemplate));
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
            TabViewItemDataTemplate item = new() { Header = "New Tab", Content = new Page(), Symbol = Symbol.View };
            TabView.TabItems.Add(CreateNewTab(item));
        }

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private TabViewItem CreateNewTab(TabViewItemDataTemplate i)
        {
            TabViewItem newItem = new()
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
