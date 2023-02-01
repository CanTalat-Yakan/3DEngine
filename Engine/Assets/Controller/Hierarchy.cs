using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Engine.Components;
using Engine.ECS;
using Engine.Utilities;
using Windows.System.RemoteSystems;

namespace Editor.Controller
{
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

            Properties.Clear();
            Properties.Set(new ModelView.Properties(entity));
        }

        private void CreateDefaultHierarchy()
        {
            _stackPanel.Children.Add(CreateSceneHierarchy(SceneEntry).StackInGrid().WrapInExpander("Scene").AddContentFlyout(CreateRootMenuFlyout()));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateButton("Add Subscene", (s, e) => ContentDialogCreateNewSubscene()));
            _stackPanel.Children.Add(CreateSubsceneHierarchy(out SceneEntry subSceneEntry)
                .StackInGrid().WrapInExpanderWithToggleButton("Subscene", SceneManager.GetFromID(subSceneEntry.ID), "IsEnabled")
                .AddContentFlyout(CreateSubRootMenuFlyout(subSceneEntry)));
        }

        public void PopulateTree(SceneEntry sceneEntry)
        {
            Scene scene = SceneManager.GetFromID(sceneEntry.ID);

            scene.EntitytManager.EntityList.OnAddEvent += (s, e) => AddTreeEntry(sceneEntry, (Entity)e);
            scene.EntitytManager.EntityList.OnRemoveEvent += (s, e) => RemoveTreeEntry(sceneEntry, (Entity)e);

            foreach (var entity in scene.EntitytManager.EntityList)
                AddTreeEntry(sceneEntry, entity);
        }

        public void DeselectTreeViewNodes()
        {
            SceneEntry.TreeView.SelectedNode = null;

            foreach (var subsceneTreeView in SubsceneEntries)
                subsceneTreeView.TreeView.SelectedNode = null;
        }

        public TreeEntry GetParent(TreeEntry treeEntry)
        {
            if (treeEntry.IDparent != null)
                foreach (var item in SceneEntry.Hierarchy)
                    if (item.ID == treeEntry.IDparent.Value)
                        return item;

            return null;
        }

        public TreeEntry[] GetChildren(TreeEntry treeEntry)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in SceneEntry.Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == treeEntry.ID)
                        list.Add(item);

            return list.ToArray();
        }

        public TreeEntry GetTreeEntry(Guid guid, SceneEntry sceneEntry = null)
        {
            if (sceneEntry != null)
                foreach (var entry in sceneEntry.Hierarchy)
                    if (entry.ID == guid)
                        return entry;

            foreach (var entry in SceneEntry.Hierarchy)
                if (entry.ID == guid)
                    return entry;

            if (SubsceneEntries != null)
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

            if (SubsceneEntries != null)
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

            if (SubsceneEntries != null)
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
            if (scene is null) scene = SceneManager.Scene;

            var sceneGrid = new Grid[]
            {
                CreateTreeView(out sceneEntry.TreeView, _hierarchy.Resources["x_TreeViewIconNodeTemplateSelector"] as TreeViewIconNodeTemplateSelector),
                CreateButton("Create Entity", (s, e) => scene.EntitytManager.CreateEntity() )
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
            TreeEntry treeEntry = new() { Name = entity.Name, ID = entity.ID };
            treeEntry.IconNode = new() { Name = treeEntry.Name, TreeEntry = treeEntry, IsExpanded = false };
            treeEntry.IconNode.IsActive = true;
            treeEntry.IDparent = entity.Parent != null ? entity.Parent.ID : null;

            var components = entity.GetComponents();
            treeEntry.IconNode.Camera = components.OfType<Camera>().Any();
            treeEntry.IconNode.Mesh = components.OfType<Mesh>().Any();
            treeEntry.IconNode.ScriptsCount = components.Length;

            sceneEntry.Hierarchy.Add(treeEntry);

            TreeEntry parent;
            if ((parent = GetParent(treeEntry)) != null)
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

            items[3].Click += (s, e) => ContentDialogRename(_itemInvoked);
            items[3].KeyboardAccelerators.Add(new KeyboardAccelerator() { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F2 });
            items[4].Click += (s, e) => ContentDialogDelete(_itemInvoked);
            items[4].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Delete });

            items[5].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity(GetEntity(GetParent(_itemInvoked)));
            items[6].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity(GetEntity(_itemInvoked));

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

            items[3].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity();

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
                new MenuFlyoutItem() { Text = "Unload" },
                new MenuFlyoutItem() { Text = "Load" },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };
            items[2].Click += (s, e) => PasteEntityFromClipboard(sceneEntry);

            items[5].Click += (s, e) => SceneManager.GetFromID(sceneEntry.ID).EntitytManager.CreateEntity();

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                menuFlyout.Items.Add(item);

                if (item.Text == "Show in Files"
                    || item.Text == "Paste"
                    || item.Text == "Load")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout, sceneEntry);

            return menuFlyout;
        }

        private MenuFlyout AppendDynamicMenuFlyoutSubItems(MenuFlyout menuFlyout, SceneEntry sceneEntry = null)
        {
            MenuFlyoutSubItem objectSubItem = new() { Text = "Mesh" };
            foreach (var type in Enum.GetNames(typeof(EPrimitiveTypes)))
            {
                MenuFlyoutItem item = new() { Text = type.ToString().FormatString() };
                if (sceneEntry != null)
                    item.Click += (s, e) => SceneManager.GetFromID(sceneEntry.ID).EntitytManager.CreatePrimitive((EPrimitiveTypes)Enum.Parse(typeof(EPrimitiveTypes), type));
                else
                    item.Click += (s, e) => SceneManager.Scene.EntitytManager.CreatePrimitive((EPrimitiveTypes)Enum.Parse(typeof(EPrimitiveTypes), type));

                objectSubItem.Items.Add(item);
            }

            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            menuFlyout.Items.Add(objectSubItem);
            menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Camera" });

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
                    .StackInGrid().WrapInExpanderWithToggleButton(subsceneName.Text, SceneManager.GetFromID(subsceneEntry.ID), "IsEnabled")
                    .AddContentFlyout(CreateSubRootMenuFlyout(subsceneEntry)));
            }
        }

        private async void ContentDialogRename(TreeEntry treeEntry)
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

        private async void ContentDialogDelete(TreeEntry treeEntry)
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

                scene.EntitytManager.Destroy(GetEntity(treeEntry.ID));

                foreach (var iconNode in treeEntry.IconNode.Children)
                {
                    scene.EntitytManager.Destroy(GetEntity(iconNode.TreeEntry.ID));
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
            if (sceneEntry != null)
                foreach (var entity in SceneManager.GetFromID(sceneEntry.ID).EntitytManager.EntityList)
                    if (entity != null)
                        if (entity.ID == guid)
                            return entity;

            foreach (var entity in SceneManager.Scene.EntitytManager.EntityList)
                if (entity != null)
                    if (entity.ID == guid)
                        return entity;

            foreach (var subscene in SceneManager.Subscenes)
                foreach (var entity in subscene.EntitytManager.EntityList)
                    if (entity != null)
                        if (entity.ID == guid)
                            return entity;

            return null;
        }

        public Entity GetEntity(TreeEntry entry, SceneEntry sceneEntry = null)
        {
            if (entry is null)
                return null;

            return GetEntity(entry.ID, sceneEntry);
        }

        public void GetEntity(out Entity entity, TreeEntry entry, SceneEntry sceneEntry = null) => entity = GetEntity(entry.ID, sceneEntry);

        public void GetEntity(out Entity entity, Guid guid, SceneEntry sceneEntry = null) => entity = GetEntity(guid, sceneEntry);

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

            if (sourceEntity != null)
                if (requestedOperation == DataPackageOperation.Move)
                {
                    sourceTreeEntry.IDparent = targetTreeEntry.ID;
                    sourceEntity.Parent = targetEntity;

                    MigrateIconNode(sourceTreeEntry, sourceSceneEntry, targetTreeEntry, null);
                    MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
                }
                else if (requestedOperation == DataPackageOperation.Copy)
                {
                    var newEntity = SceneManager.GetFromID(sourceSceneEntry.ID).EntitytManager.Duplicate(sourceEntity, targetEntity);
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

            if (sourceEntity != null)
                if (requestedOperation == DataPackageOperation.Move)
                {
                    sourceTreeEntry.IDparent = null;
                    sourceEntity.Parent = null;

                    MigrateIconNode(sourceTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                    MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
                }
                else if (requestedOperation == DataPackageOperation.Copy)
                {
                    var newEntity = SceneManager.GetFromID(sourceSceneEntry.ID).EntitytManager.Duplicate(sourceEntity);
                    var newTreeEntry = GetTreeEntry(newEntity.ID, sourceSceneEntry);

                    foreach (var childIconNode in sourceTreeEntry.IconNode.Children)
                    {
                        GetEntity(out Entity childEntity, sourceScene.EntitytManager.GetFromID(childIconNode.TreeEntry.ID).ID, sourceSceneEntry);
                        PasteEntity(childEntity.ID, newEntity.ID, DataPackageOperation.Copy);
                    }

                    MigrateIconNode(newTreeEntry, sourceSceneEntry, null, targetSceneEntry);
                    MigrateEntityRecurisivally(sourceScene, targetScene, sourceTreeEntry);
                }
        }

        public void MigrateIconNode(TreeEntry sourceTreeEntry, SceneEntry sourceSceneEntry, TreeEntry targetTreeEntry, SceneEntry targetSceneEntry)
        {
            var parent = GetParent(sourceTreeEntry);
            if (parent != null)
                parent.IconNode.Children.Remove(sourceTreeEntry.IconNode);
            else
                sourceSceneEntry.DataSource.Remove(sourceTreeEntry.IconNode);

            if (targetTreeEntry != null)
                targetTreeEntry.IconNode.Children.Add(sourceTreeEntry.IconNode);
            else if (targetSceneEntry != null)
                targetSceneEntry.DataSource.Add(sourceTreeEntry.IconNode);
        }

        public void MigrateEntityRecurisivally(Scene sourceScene, Scene targetScene, params TreeEntry[] treeEntries)
        {
            if (sourceScene != targetScene)
                foreach (var treeEntry in treeEntries)
                {
                    if (treeEntry.IconNode.Children.Count != 0)
                        MigrateEntityRecurisivally(sourceScene, targetScene, treeEntry.IconNode.Children.Select(TreeViewIconNode => TreeViewIconNode.TreeEntry).ToArray());

                    targetScene.EntitytManager.EntityList.Add(sourceScene.EntitytManager.GetFromID(treeEntry.ID), false);
                    sourceScene.EntitytManager.EntityList.Remove(sourceScene.EntitytManager.GetFromID(treeEntry.ID), false);

                    targetScene.EntitytManager.GetFromID(treeEntry.ID).Scene = targetScene;
                }
        }
    }
}
