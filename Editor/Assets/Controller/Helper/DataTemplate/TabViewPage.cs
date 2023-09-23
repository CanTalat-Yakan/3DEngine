using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Editor.Controller;

internal sealed class TabViewItemDataTemplate
{
    public string Header;
    public object Content;
    public Symbol Symbol;
}

internal sealed class TabViewPage
{
    public TabView TabView;

    public TabViewPage(params TabViewItemDataTemplate[] tabViewDataTemplate)
    {
        // Initialize the TabView control.
        TabView = new TabView
        {
            TabWidthMode = TabViewWidthMode.SizeToContent,
            CloseButtonOverlayMode = TabViewCloseButtonOverlayMode.Auto,
            IsAddTabButtonVisible = true,
            Padding = new Thickness(8, 8, 8, 0)
        };
        // Subscribe to the add tab button click event.
        TabView.AddTabButtonClick += TabView_AddButtonClick;

        // Iterate through the data templates and create new tabs for each.
        foreach (var dataTemplate in tabViewDataTemplate)
            TabView.TabItems.Add(CreateNewTab(dataTemplate));
    }

    private void TabView_AddButtonClick(TabView sender, object args)
    {
        // Create a new TabViewItemDataTemplate with the header "New Tab", content set to a new Page, and symbol set to Symbol.View.
        TabViewItemDataTemplate item = new() { Header = "New Tab", Content = new Page(), Symbol = Symbol.View };

        // Add a new tab to the TabView with the TabViewItemDataTemplate item.
        TabView.TabItems.Add(CreateNewTab(item));
    }

    private TabViewItem CreateNewTab(TabViewItemDataTemplate i)
    {
        // Create a new TabViewItem and set its properties.
        TabViewItem newItem = new()
        {
            Header = i.Header + "⠀⠀⠀⠀⠀⠀⠀⠀", // Add spaces to the header text.
            Content = i.Content,
            IconSource = new SymbolIconSource() { Symbol = i.Symbol },
            IsClosable = false // Disable the close button for this tab.
        };

        // Return the new TabViewItem.
        return newItem;
    }
}
