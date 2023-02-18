using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace Editor.Controller;

internal class TreeEntry
{
    public Guid ID;
    public Guid? IDparent;
    public string Name;

    public TreeViewIconNode IconNode;
}

internal class SceneEntry
{
    public Guid ID;
    public string Name;

    public TreeView TreeView;
    public List<TreeEntry> Hierarchy;
    public ObservableCollection<TreeViewIconNode> DataSource;
    public Grid Content;
}

internal partial class Hierarchy
{
    public SceneEntry SceneEntry;
    public List<SceneEntry> SubsceneEntries;

    private ModelView.Hierarchy _hierarchy;
    private StackPanel _stackPanel;

    private TreeEntry _itemInvoked = null;

    public Hierarchy(ModelView.Hierarchy hierarchy, StackPanel stackPanel)
    {
        _hierarchy = hierarchy;
        _stackPanel = stackPanel;

        SceneEntry = new() { ID = SceneManager.Scene.ID, Name = SceneManager.Scene.Name, Hierarchy = new(), DataSource = new() };
        SubsceneEntries = new();

        CreateDefaultHierarchy();
    }

    public void SetProperties(TreeView treeView)
    {
        if (treeView.SelectedNode is null)
            return;

        var selectedNode = treeView.SelectedNode;

        DeselectTreeViewNodes();
        treeView.SelectedNode = selectedNode;

        var treeViewIconNode = (TreeViewIconNode)selectedNode.Content;
        var entity = GetEntity(treeViewIconNode.TreeEntry);

        Properties.Set(entity);
    }

    private void CreateDefaultHierarchy()
    {
        _stackPanel.Children.Add(CreateSceneHierarchy(SceneEntry).StackInGrid().WrapInExpander("Scene").AddContentFlyout(CreateRootMenuFlyout()));
        _stackPanel.Children.Add(CreateSeperator());
        _stackPanel.Children.Add(CreateButton("Add Subscene", (s, e) => ContentDialogCreateNewSubscene()));
        _stackPanel.Children.Add(CreateSubsceneHierarchy(out SceneEntry subSceneEntry)
            .StackInGrid().WrapInExpanderWithToggleButton(ref subSceneEntry.Content, "Subscene", SceneManager.GetFromID(subSceneEntry.ID), "IsEnabled", "Name")
            .AddContentFlyout(CreateSubRootMenuFlyout(subSceneEntry)));
    }

    public void PopulateTree(SceneEntry sceneEntry)
    {
        Scene scene = SceneManager.GetFromID(sceneEntry.ID);

        scene.EntityManager.EntityList.OnAdd += (s, e) => AddTreeEntry(sceneEntry, (Entity)e);
        scene.EntityManager.EntityList.OnRemove += (s, e) => RemoveTreeEntry(sceneEntry, (Entity)e);

        foreach (var entity in scene.EntityManager.EntityList)
            AddTreeEntry(sceneEntry, entity);
    }

    public void DeselectTreeViewNodes()
    {
        SceneEntry.TreeView.SelectedNode = null;

        foreach (var subsceneTreeView in SubsceneEntries)
            subsceneTreeView.TreeView.SelectedNode = null;
    }

    public TreeEntry GetParent(TreeEntry treeEntry, SceneEntry sceneEntry = null)
    {
        if (treeEntry.IDparent is null)
            return null;

        List<TreeEntry> hierarchy;
        if (sceneEntry is not null)
            hierarchy = sceneEntry.Hierarchy;
        else
            hierarchy = GetSceneEntry(treeEntry).Hierarchy;

        foreach (var entry in hierarchy)
            if (entry.ID == treeEntry.IDparent.Value)
                return entry;

        return null;
    }

    public TreeEntry[] GetChildren(TreeEntry treeEntry, SceneEntry sceneEntry = null)
    {
        List<TreeEntry> list = new List<TreeEntry>();

        List<TreeEntry> hierarchy;
        if (sceneEntry is not null)
            hierarchy = sceneEntry.Hierarchy;
        else
            hierarchy = GetSceneEntry(treeEntry).Hierarchy;

        foreach (var entry in hierarchy)
            if (entry.IDparent is not null)
                if (entry.IDparent.Value == treeEntry.ID)
                    list.Add(entry);

        return list.ToArray();
    }

    public TreeEntry GetTreeEntry(Guid guid, SceneEntry sceneEntry = null)
    {
        if (sceneEntry is not null)
            foreach (var entry in sceneEntry.Hierarchy)
                if (entry.ID == guid)
                    return entry;

        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == guid)
                return entry;

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var treeEntry in subSceneEntry.Hierarchy)
                    if (treeEntry.ID == guid)
                        return treeEntry;

        return null;
    }

    public SceneEntry GetSceneEntry(TreeEntry treeEntry)
    {
        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == treeEntry.ID)
                return SceneEntry;

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var entry in subSceneEntry.Hierarchy)
                    if (entry.ID == treeEntry.ID)
                        return subSceneEntry;

        return null;
    }

    public void GetEntries(out TreeEntry treeEntry, out SceneEntry sceneEntry, Guid guid)
    {
        treeEntry = null;
        sceneEntry = null;

        foreach (var entry in SceneEntry.Hierarchy)
            if (entry.ID == guid)
            {
                treeEntry = entry;
                sceneEntry = SceneEntry;
            }

        if (SubsceneEntries is not null)
            foreach (var subSceneEntry in SubsceneEntries)
                foreach (var entry in subSceneEntry.Hierarchy)
                    if (entry.ID == guid)
                    {
                        treeEntry = entry;
                        sceneEntry = subSceneEntry;

                        return;
                    }
    }

    private void GetInvokedItemAndSetContextFlyout(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint((UIElement)sender).Properties;
        if (properties.IsRightButtonPressed)
        {
            var dc = ((FrameworkElement)e.OriginalSource).DataContext;
            if (dc is null)
                return;

            _itemInvoked = ((TreeViewIconNode)dc).TreeEntry;

            ((TreeView)sender).ContextFlyout = CreateDefaultMenuFlyout();
            ((TreeView)sender).ContextFlyout.Opened += (s, e) => ((TreeView)sender).ContextFlyout = null;
        }
    }

    private Grid[] CreateSceneHierarchy(in SceneEntry sceneEntry, Scene scene = null)
    {
        if (scene is null)
            scene = SceneManager.Scene;

        var sceneGrid = new Grid[]
        {
                CreateTreeView(out sceneEntry.TreeView, _hierarchy.Resources["x_TreeViewIconNodeTemplateSelector"] as TreeViewIconNodeTemplateSelector),
                CreateButton("Create Entity", (s, e) => scene.EntityManager.CreateEntity() )
        };
        sceneEntry.TreeView.ItemsSource = sceneEntry.DataSource;
        sceneEntry.TreeView.PointerPressed += (s, e) => GetInvokedItemAndSetContextFlyout(s, e);
        sceneEntry.TreeView.Tapped += (s, e) => SetProperties((TreeView)s);
        sceneEntry.TreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewIconNode)e.NewParentItem, e.Items.Cast<TreeViewIconNode>().ToArray());

        PopulateTree(sceneEntry);

        return sceneGrid;
    }

    private Grid[] CreateSubsceneHierarchy(out SceneEntry subsceneEntry, string name = "Subscene", bool enable = true)
    {
        subsceneEntry = new SceneEntry() { ID = Guid.NewGuid(), Name = name, Hierarchy = new(), DataSource = new() };

        var subScene = SceneManager.AddSubscene(subsceneEntry.ID, name, enable);
        var subsceneGrid = CreateSceneHierarchy(subsceneEntry, subScene);

        SubsceneEntries.Add(subsceneEntry);

        return subsceneGrid;
    }

    private TreeEntry AddTreeEntry(SceneEntry sceneEntry, Entity entity)
    {
        if (entity.CompareTag(Enum.GetNames(typeof(EditorTags))))
            return null;

        TreeEntry treeEntry = new() { Name = entity.Name, ID = entity.ID };
        treeEntry.IconNode = new() { Name = treeEntry.Name, TreeEntry = treeEntry, IsExpanded = false };
        treeEntry.IconNode.IsActive = true;
        treeEntry.IDparent = entity.Parent is not null ? entity.Parent.ID : null;

        var components = entity.GetComponents();
        treeEntry.IconNode.Camera = components.OfType<Camera>().Any();
        treeEntry.IconNode.Mesh = components.OfType<Mesh>().Any();
        treeEntry.IconNode.ScriptsCount = components.Length;

        sceneEntry.Hierarchy.Add(treeEntry);

        TreeEntry parent;
        if ((parent = GetParent(treeEntry, sceneEntry)) is not null)
            parent.IconNode.Children.Add(treeEntry.IconNode);
        else
            sceneEntry.DataSource.Add(treeEntry.IconNode);

        return treeEntry;
    }

    private void RemoveTreeEntry(SceneEntry sceneEntry, Entity entity)
    {
        var treeEntry = GetTreeEntry(entity.ID);

        TreeEntry parent;
        if ((parent = GetParent(treeEntry)) is null)
            sceneEntry.DataSource.Remove(treeEntry.IconNode);
        else
            parent.IconNode.Children.Remove(treeEntry.IconNode);

        sceneEntry.Hierarchy.Remove(treeEntry);
    }

    private MenuFlyout CreateDefaultMenuFlyout()
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
                new MenuFlyoutItem() { Text = "Create Child Entity" },
            };
        items[0].Click += (s, e) => CopyToClipboard(_itemInvoked.ID, DataPackageOperation.Move);
        items[0].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.X });
        items[1].Click += (s, e) => CopyToClipboard(_itemInvoked.ID, DataPackageOperation.Copy);
        items[1].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.C });
        items[2].Click += (s, e) => PasteEntityFromClipboard(_itemInvoked.ID);
        items[2].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.V });

        items[3].Click += (s, e) => ContentDialogRenameTreeEntry(_itemInvoked);
        items[3].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F2 });
        items[4].Click += (s, e) => ContentDialogDeleteTreeEntry(_itemInvoked);
        items[4].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Delete });

        items[5].Click += (s, e) =>
        {
            var entity = GetEntity(_itemInvoked);
            entity.Scene.EntityManager.CreateEntity(entity.Parent);
        };
        items[6].Click += (s, e) =>
        {
            var entity = GetEntity(_itemInvoked);
            entity.Scene.EntityManager.CreateEntity(entity);
        };

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Paste"
                || item.Text == "Delete")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout);

        return menuFlyout;
    }

    private MenuFlyout CreateRootMenuFlyout()
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };
        items[2].Click += (s, e) => PasteEntityFromClipboard(SceneEntry);
        items[2].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.V });

        items[3].Click += (s, e) => SceneManager.Scene.EntityManager.CreateEntity();

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Show in Files"
                || item.Text == "Paste")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout);

        return menuFlyout;
    }

    private MenuFlyout CreateSubRootMenuFlyout(SceneEntry sceneEntry = null)
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Unload" },
                new MenuFlyoutItem() { Text = "Load" },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };
        items[2].Click += (s, e) => PasteEntityFromClipboard(sceneEntry);

        items[3].Click += (s, e) => ContentDialogRenameSubscene(sceneEntry);
        items[4].Click += (s, e) => ContentDialogDeleteSubscene(sceneEntry);

        items[7].Click += (s, e) => SceneManager.GetFromID(sceneEntry.ID).EntityManager.CreateEntity();

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Show in Files"
                || item.Text == "Paste"
                || item.Text == "Delete"
                || item.Text == "Load")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout, sceneEntry);

        return menuFlyout;
    }

    private MenuFlyout AppendDynamicMenuFlyoutSubItems(MenuFlyout menuFlyout, SceneEntry sceneEntry = null)
    {
        MenuFlyoutItem item;

        MenuFlyoutSubItem objectSubItem = new() { Text = "Mesh" };
        foreach (var type in Enum.GetNames(typeof(PrimitiveTypes)))
        {
            item = new() { Text = type.ToString().FormatString() };
            item.Click += (s, e) =>
            {
                if (_itemInvoked is not null)
                {
                    var entity = GetEntity(_itemInvoked);
                    entity.Scene.EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type), entity);
                }
                else if (sceneEntry is not null)
                    SceneManager.GetFromID(sceneEntry.ID).EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type));
                else
                    SceneManager.Scene.EntityManager.CreatePrimitive((PrimitiveTypes)Enum.Parse(typeof(PrimitiveTypes), type));
            };

            objectSubItem.Items.Add(item);
        }

        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        menuFlyout.Items.Add(objectSubItem);
        menuFlyout.Items.Add(item = new MenuFlyoutItem() { Text = "Camera" });
        item.Click += (s, e) =>
        {
            if (_itemInvoked is not null)
            {
                var entity = GetEntity(_itemInvoked);
                entity.Scene.EntityManager.CreateCamera("Camera", Tags.MainCamera.ToString(), entity);
            }
            else if (sceneEntry is not null)
                SceneManager.GetFromID(sceneEntry.ID).EntityManager.CreateCamera();
            else
                SceneManager.Scene.EntityManager.CreateCamera();
        };

        return menuFlyout;
    }

    private async void ContentDialogCreateNewSubscene()
    {
        TextBox subsceneName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Create new Subscene",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = subsceneName = new TextBox() { Text = "Subscene" },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(subsceneName.Text))
                if (!Regex.Match(subsceneName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "A subscene can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            subsceneName.Text = subsceneName.Text.IncrementNameIfExists(SceneManager.Subscenes.ToArray().Select(Scene => Scene.Name).ToArray());

            _stackPanel.Children.Add(CreateSubsceneHierarchy(out SceneEntry subsceneEntry, subsceneName.Text)
                .StackInGrid().WrapInExpanderWithToggleButton(ref subsceneEntry.Content, subsceneName.Text, SceneManager.GetFromID(subsceneEntry.ID), "IsEnabled", "Name")
                .AddContentFlyout(CreateSubRootMenuFlyout(subsceneEntry)));
        }
    }

    private async void ContentDialogRenameSubscene(SceneEntry sceneEntry)
    {
        TextBox fileName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Rename",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = fileName = new TextBox() { Text = sceneEntry.Name },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(fileName.Text))
                if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "An entity can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            Scene scene = SceneManager.GetFromID(sceneEntry.ID);

            scene.Name = fileName.Text;
            sceneEntry.Name = fileName.Text;
        }
    }

    private async void ContentDialogRenameTreeEntry(TreeEntry treeEntry)
    {
        TextBox fileName;

        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Rename",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = fileName = new TextBox() { Text = treeEntry.Name },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(fileName.Text))
                if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _hierarchy.XamlRoot,
                        Title = "An entity can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            GetEntity(treeEntry.ID).Name = fileName.Text;
            treeEntry.Name = fileName.Text;
            treeEntry.IconNode.TreeEntry = treeEntry;
        }
    }

    private async void ContentDialogDeleteSubscene(SceneEntry sceneEntry)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Delete " + sceneEntry.Name,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            sceneEntry.DataSource.Clear();
            sceneEntry.Hierarchy.Clear();
            sceneEntry.Content.Children.Clear();
            _stackPanel.Children.Remove(sceneEntry.Content);

            SceneManager.RemoveSubscene(sceneEntry.ID);
        }
    }

    private async void ContentDialogDeleteTreeEntry(TreeEntry treeEntry)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = _hierarchy.XamlRoot,
            Title = "Delete " + treeEntry.Name,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SceneEntry sceneEntry = GetSceneEntry(treeEntry);
            Scene scene = SceneManager.GetFromID(sceneEntry.ID);

            scene.EntityManager.Destroy(GetEntity(treeEntry.ID));

            foreach (var iconNode in treeEntry.IconNode.Children)
            {
                scene.EntityManager.Destroy(GetEntity(iconNode.TreeEntry.ID));
                sceneEntry.DataSource.Remove(iconNode);
            }

            sceneEntry.DataSource.Remove(treeEntry.IconNode);
        }
    }
}

internal partial class Hierarchy : Controller.Helper
{
    public Entity GetEntity(Guid guid, SceneEntry sceneEntry = null)
    {
        Entity entity;

        if (sceneEntry is not null)
            entity = SceneManager.GetFromID(sceneEntry.ID).EntityManager.GetFromID(guid);
        else
        {
            entity = SceneManager.Scene.EntityManager.GetFromID(guid);

            if (entity is null)
                foreach (var subscene in SceneManager.Subscenes)
                    if (entity is null)
                        entity = subscene.EntityManager.GetFromID(guid);
                    else break;
        }

        return entity;
    }

    public Entity GetEntity(TreeEntry entry, SceneEntry sceneEntry = null)
    {
        if (entry is null)
            return null;

        return GetEntity(entry.ID, sceneEntry);
    }

    public void GetEntity(out Entity entity, TreeEntry entry, SceneEntry sceneEntry = null) =>
        entity = GetEntity(entry.ID, sceneEntry);

    public void GetEntity(out Entity entity, Guid guid, SceneEntry sceneEntry = null) =>
        entity = GetEntity(guid, sceneEntry);

    public void GetScenes(out Scene sourceScene, out Scene targetScene, SceneEntry sourceSceneEntry, SceneEntry targetSceneEntry)
    {
        sourceScene = SceneManager.GetFromID(sourceSceneEntry.ID);
        targetScene = SceneManager.GetFromID(targetSceneEntry.ID);
    }

    public void SetNewParentTreeEntry(TreeViewIconNode newParent, params TreeViewIconNode[] treeViewIconNodes)
    {
        if (newParent is null)
            return;

        foreach (var node in treeViewIconNodes)
        {
            (newParent.TreeEntry).IDparent = (node.TreeEntry).ID;
            GetEntity(node.TreeEntry).Parent = GetEntity(newParent.TreeEntry);
        }
    }

    private void CopyToClipboard(Guid guid, DataPackageOperation requestedOpertion)
    {
        DataPackage data = new();
        data.SetText(guid.ToString());
        data.RequestedOperation = requestedOpertion;

        Clipboard.SetContent(data);
    }

    public async void PasteEntityFromClipboard(Guid guid)
    {
        DataPackageView dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            var sourceText = await dataPackageView.GetTextAsync();
            if (Guid.TryParse(sourceText, out Guid sourceGuid))
                PasteEntity(sourceGuid, guid, dataPackageView.RequestedOperation);
        }
    }

    public async void PasteEntityFromClipboard(SceneEntry sceneEntry)
    {
        DataPackageView dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.Text))
        {
            var sourceText = await dataPackageView.GetTextAsync();
            if (Guid.TryParse(sourceText, out Guid sourceGuid))
                PasteEntity(sourceGuid, sceneEntry, dataPackageView.RequestedOperation);
        }
    }

    public void PasteEntity(Guid sourceEntityGuid, Guid targetEntityGuid, DataPackageOperation requestedOperation)
    {
        GetEntries(out TreeEntry sourceTreeEntry, out SceneEntry sourceSceneEntry, sourceEntityGuid);
        GetEntity(out Entity sourceEntity, sourceEntityGuid, sourceSceneEntry);

        GetEntries(out TreeEntry targetTreeEntry, out SceneEntry targetSceneEntry, targetEntityGuid);
        GetEntity(out Entity targetEntity, targetEntityGuid, targetSceneEntry);

        GetScenes(out Scene sourceScene, out Scene targetScene, sourceSceneEntry, targetSceneEntry);

        if (sourceEntity is not null)
            if (requestedOperation == DataPackageOperation.Move)
            {
                sourceTreeEntry.IDparent = targetTreeEntry.ID;
                sourceEntity.Parent = targetEntity;

                MigrateIconNode(sourceTreeEntry, sourceSceneEntry, targetTreeEntry, null);
                MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
            }
            else if (requestedOperation == DataPackageOperation.Copy)
            {
                var newEntity = SceneManager.GetFromID(sourceSceneEntry.ID).EntityManager.Duplicate(sourceEntity, targetEntity);
                var newTreeEntry = GetTreeEntry(newEntity.ID, sourceSceneEntry);

                MigrateIconNode(newTreeEntry, sourceSceneEntry, targetTreeEntry, null);
                MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
            }
    }

    public void PasteEntity(Guid sourceEntityGuid, SceneEntry targetSceneEntry, DataPackageOperation requestedOperation)
    {
        GetEntries(out TreeEntry sourceTreeEntry, out SceneEntry sourceSceneEntry, sourceEntityGuid);
        GetEntity(out Entity sourceEntity, sourceEntityGuid, sourceSceneEntry);
        GetScenes(out Scene sourceScene, out Scene targetScene, sourceSceneEntry, targetSceneEntry);

        if (sourceEntity is not null)
            if (requestedOperation == DataPackageOperation.Move)
            {
                sourceTreeEntry.IDparent = null;
                sourceEntity.Parent = null;

                MigrateIconNode(sourceTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
            }
            else if (requestedOperation == DataPackageOperation.Copy)
            {
                var newEntity = SceneManager.GetFromID(sourceSceneEntry.ID).EntityManager.Duplicate(sourceEntity);
                var newTreeEntry = GetTreeEntry(newEntity.ID, sourceSceneEntry);

                foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                {
                    GetEntity(out Entity childEntity, sourceScene.EntityManager.GetFromID(childIconNode.TreeEntry.ID).ID, sourceSceneEntry);
                    PasteEntity(childEntity.ID, newEntity.ID, DataPackageOperation.Copy);
                }

                MigrateIconNode(newTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
            }
    }

    public void MigrateIconNode(TreeEntry sourceTreeEntry, SceneEntry sourceSceneEntry, TreeEntry targetTreeEntry, SceneEntry targetSceneEntry)
    {
        var parent = GetParent(sourceTreeEntry);
        if (parent is not null)
            parent.IconNode.Children.Remove(sourceTreeEntry.IconNode);
        else
            sourceSceneEntry.DataSource.Remove(sourceTreeEntry.IconNode);

        if (targetTreeEntry is not null)
            targetTreeEntry.IconNode.Children.Add(sourceTreeEntry.IconNode);
        else if (targetSceneEntry is not null)
            targetSceneEntry.DataSource.Add(sourceTreeEntry.IconNode);
    }

    public void MigrateEntityRecurisivally(Scene sourceScene, Scene targetScene, params TreeEntry[] treeEntries)
    {
        if (sourceScene != targetScene)
            foreach (var treeEntry in treeEntries)
            {
                if (treeEntry.IconNode.Children.Count != 0)
                    MigrateEntityRecurisivally(sourceScene, targetScene, treeEntry.IconNode.Children.Select(TreeViewIconNode => TreeViewIconNode.TreeEntry).ToArray());

                targetScene.EntityManager.EntityList.Add(sourceScene.EntityManager.GetFromID(treeEntry.ID), false);
                sourceScene.EntityManager.EntityList.Remove(sourceScene.EntityManager.GetFromID(treeEntry.ID), false);

                targetScene.EntityManager.GetFromID(treeEntry.ID).Scene = targetScene;
            }
    }
}
