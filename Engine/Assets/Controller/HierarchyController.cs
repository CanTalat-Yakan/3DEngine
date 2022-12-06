using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using Editor.UserControls;
using Engine.Utilities;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Diagnostics.Runtime;
using System.IO;
using System.Text.RegularExpressions;
using static Assimp.Metadata;
using WinRT;
using Vortice;

namespace Editor.Controller
{
    internal class TreeEntry
    {
        public Guid ID;
        public Guid? IDparent;
        public string Name;

        public TreeViewNode Node;

        public override string ToString()
        {
            return Name;
        }
    }

    internal class SceneEntry
    {
        public Guid ID;
        public string Name;

        public TreeView TreeView;
        public List<TreeEntry> Hierarchy;

        public override string ToString()
        {
            return Name;
        }
    }

    internal partial class HierarchyController
    {
        public SceneEntry SceneEntry;
        public List<SceneEntry> SubsceneEntries;

        private Hierarchy _hierarchy;
        private StackPanel _stackPanel;

        private TreeEntry _itemInvoked = null;

        public HierarchyController(Hierarchy hierarchy, StackPanel stackPanel)
        {
            _hierarchy = hierarchy;
            _stackPanel = stackPanel;

            SceneEntry = new SceneEntry() { ID = SceneManager.Scene.ID, Name = SceneManager.Scene.Name, Hierarchy = new List<TreeEntry>() };
            SubsceneEntries = new List<SceneEntry>();

            CreateDefaultHierarchy();

            PopulateTree(SceneEntry);
        }

        private void CreateDefaultHierarchy()
        {
            _stackPanel.Children.Add(CreateSceneTreeView(SceneEntry).StackInGrid().WrapInExpander("Scene").AddContentFlyout(CreateRootMenuFlyout()));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateButton("Add Subscene", (s, e) => ContentDialogCreateNewSubscene()));
            _stackPanel.Children.Add(CreateSubsceneTreeView(out SceneEntry subSceneEntry).StackInGrid().WrapInExpanderWithToggleButton("Subscene").AddContentFlyout(CreateSubRootMenuFlyout(subSceneEntry)));
        }

        private void GetInvokedItemAndSetContextFlyout(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsRightButtonPressed)
            {
                var dc = ((FrameworkElement)e.OriginalSource).DataContext;
                if (dc is null)
                    return;

                var c = ((TreeViewNode)dc).Content as TreeEntry;
                _itemInvoked = c.Node.Content as TreeEntry;

                ((TreeView)sender).ContextFlyout = CreateDefaultMenuFlyout();
                ((TreeView)sender).ContextFlyout.Opened += (s, e) => ((TreeView)sender).ContextFlyout = null;
            }
        }

        private Grid[] CreateSceneTreeView(in SceneEntry sceneEntry, Scene scene = null)
        {
            if (scene is null) scene = SceneManager.Scene;

            var sceneGrid = new Grid[]
            {
                CreateTreeView(out sceneEntry.TreeView),
                CreateButton("Create Entity", (s, e) => scene.EntitytManager.CreateEntity() )
            };
            sceneEntry.TreeView.PointerPressed += (s, e) => GetInvokedItemAndSetContextFlyout(s, e);
            sceneEntry.TreeView.Tapped += (s, e) => SetProperties((TreeView)s);
            sceneEntry.TreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewNode)e.NewParentItem, e.Items.Cast<TreeViewNode>().ToArray());

            return sceneGrid;
        }

        private Grid[] CreateSubsceneTreeView(out SceneEntry subsceneEntry, string name = "Subscene", bool enable = true)
        {
            subsceneEntry = new SceneEntry() { ID = new Guid(), Name = name, Hierarchy = new List<TreeEntry>() };
            subsceneEntry.ID = Guid.NewGuid();

            var subScene = SceneManager.AddSubscene(subsceneEntry.ID, name, enable);
            var subsceneGrid = CreateSceneTreeView(subsceneEntry, subScene);

            SubsceneEntries.Add(subsceneEntry);

            PopulateTree(subsceneEntry);

            return subsceneGrid;
        }

        public void DeselectTreeViewNodes()
        {
            SceneEntry.TreeView.SelectedNode = null;

            foreach (var subsceneTreeView in SubsceneEntries)
                subsceneTreeView.TreeView.SelectedNode = null;
        }

        public void PopulateTree(SceneEntry sceneEntry)
        {
            Scene scene = SceneManager.GetByID(sceneEntry.ID);

            scene.EntitytManager.EntityList.OnAddEvent += (s, e) => OnAdd(sceneEntry);
            scene.EntitytManager.EntityList.OnRemoveEvent += (s, e) => OnRemove(sceneEntry, e.As<Entity>());

            foreach (var entity in scene.EntitytManager.EntityList)
            {
                var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
                if (entity.Parent != null)
                    newEntry.IDparent = entity.Parent.ID;

                sceneEntry.Hierarchy.Add(newEntry);
            }

            foreach (var entry in sceneEntry.Hierarchy)
                entry.Node = new TreeViewNode() { Content = entry, IsExpanded = true };

            TreeEntry parent;
            foreach (var entry in sceneEntry.Hierarchy)
                if ((parent = GetParent(entry)) is null)
                    sceneEntry.TreeView.RootNodes.Add(entry.Node);
                else
                    parent.Node.Children.Add(entry.Node);
        }

        public void SetProperties(TreeView treeView)
        {
            var treeViewNode = treeView.SelectedNode;

            DeselectTreeViewNodes();
            treeView.SelectedNode = treeViewNode;

            var treeEntry = (TreeEntry)treeViewNode.Content;
            var entity = GetEntity(treeEntry);

            PropertiesController.Clear();
            PropertiesController.Set(new Properties(entity));
        }

        private void OnAdd(SceneEntry sceneEntry)
        {
            Scene scene = SceneManager.GetByID(sceneEntry.ID);

            var entity = scene.EntitytManager.EntityList[^1];
            var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
            if (entity.Parent != null)
                newEntry.IDparent = entity.Parent.ID;

            newEntry.Node = new TreeViewNode() { Content = newEntry, IsExpanded = true };
            sceneEntry.Hierarchy.Add(newEntry);

            TreeEntry parent;
            if ((parent = GetParent(newEntry)) is null)
                sceneEntry.TreeView.RootNodes.Add(newEntry.Node);
            else
                parent.Node.Children.Add(newEntry.Node);
        }

        private void OnRemove(SceneEntry sceneEntry, Entity entity)
        {
            var entry = GetTreeEntry(entity.ID);

            sceneEntry.Hierarchy.Remove(entry);

            TreeEntry parent;
            if ((parent = GetParent(entry)) is null)
                sceneEntry.TreeView.RootNodes.Remove(entry.Node);
            else
                parent.Node.Children.Remove(entry.Node);
        }
    }

    internal partial class HierarchyController : HelperController
    {
        public TreeEntry GetParent(TreeEntry node)
        {
            if (node.IDparent != null)
                foreach (var item in SceneEntry.Hierarchy)
                    if (item.ID == node.IDparent.Value)
                        return item;

            return null;
        }

        public TreeEntry[] GetChildren(TreeEntry node)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in SceneEntry.Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == node.ID)
                        list.Add(item);

            return list.ToArray();
        }

        public TreeEntry GetTreeEntry(Guid guid)
        {
            foreach (var treeEntry in SceneEntry.Hierarchy)
                if (treeEntry.ID == guid)
                    return treeEntry;

            if (SubsceneEntries != null)
                foreach (var subSceneEntry in SubsceneEntries)
                    foreach (var treeEntry in subSceneEntry.Hierarchy)
                        if (treeEntry.ID == guid)
                            return treeEntry;

            return null;
        }

        public Entity GetEntity(Guid guid)
        {
            foreach (var entity in SceneManager.Scene.EntitytManager.EntityList)
                if (entity.ID == guid)
                    return entity;

            foreach (var subscene in SceneManager.Subscenes)
                foreach (var entity in subscene.EntitytManager.EntityList)
                    if (entity.ID == guid)
                        return entity;

            return null;
        }

        public Entity GetEntity(TreeEntry entry)
        {
            return GetEntity(entry.ID);
        }

        public void SetNewParentTreeEntry(TreeViewNode newParent, params TreeViewNode[] treeViewNodes)
        {
            foreach (var node in treeViewNodes)
            {
                (newParent.Content as TreeEntry).IDparent = (node.Content as TreeEntry).ID;
                GetEntity(node.Content as TreeEntry).Parent = GetEntity(newParent.Content as TreeEntry);
            }
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
            items[1].Click += (s, e) => CopyToClipboard(_itemInvoked.ID, DataPackageOperation.Copy);
            items[2].Click += (s, e) => PasteEntityFromClipboard(_itemInvoked.ID);

            items[3].Click += (s, e) => ContentDialogRename(_itemInvoked);
            items[4].Click += (s, e) => ContentDialogDelete(_itemInvoked);

            items[5].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity(GetEntity(_itemInvoked.Node.Parent.Content as TreeEntry));
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

            //items[0].Click += (s, e) => OpenFolder(path);
            //items[1].Click += (s, e) => OpenFolder(path);

            items[2].Click += (s, e) => PasteEntityFromClipboard(SceneEntry);

            items[3].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity();

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                menuFlyout.Items.Add(item);

                if (item.Text == "Show in Files"
                    || item.Text == "Delete")
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
                new MenuFlyoutItem() { Text = "Unload" },
                new MenuFlyoutItem() { Text = "Load" },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };

            //items[0].Click += (s, e) => OpenFolder(path);
            //items[1].Click += (s, e) => OpenFolder(path);

            //items[2].Click += (s, e) => ContentDialogRename(path);
            //items[3].Click += (s, e) => ContentDialogDelete(path);

            items[4].Click += (s, e) => PasteEntityFromClipboard(sceneEntry);

            items[5].Click += (s, e) => SceneManager.Scene.EntitytManager.CreateEntity();

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                menuFlyout.Items.Add(item);

                if (item.Text == "Show in Files"
                    || item.Text == "Load"
                    || item.Text == "Paste")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            menuFlyout = AppendDynamicMenuFlyoutSubItems(menuFlyout);

            return menuFlyout;
        }

        private MenuFlyout AppendDynamicMenuFlyoutSubItems(MenuFlyout menuFlyout)
        {
            MenuFlyoutItem[] objects = new[] {
                new MenuFlyoutItem() { Text = "Plane"},
                new MenuFlyoutItem() { Text = "Cube"},
                new MenuFlyoutItem() { Text = "Sphere"},
                new MenuFlyoutItem() { Text = "Cylinder"},
                new MenuFlyoutItem() { Text = "Capsule"},
                new MenuFlyoutItem() { Text = "Quad"},
            };
            var objectSubItem = new MenuFlyoutSubItem() { Text = "Objects" };
            foreach (var item in objects)
                objectSubItem.Items.Add(item);

            MenuFlyoutItem[] Lights = new[] {
                new MenuFlyoutItem() { Text = "Directional Light"},
                new MenuFlyoutItem() { Text = "Point Light"},
                new MenuFlyoutItem() { Text = "Spot Light"},
            };
            var lightSubItem = new MenuFlyoutSubItem() { Text = "Light" };
            foreach (var item in Lights)
                lightSubItem.Items.Add(item);

            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            menuFlyout.Items.Add(objectSubItem);
            menuFlyout.Items.Add(lightSubItem);

            return menuFlyout;
        }

        private async void ContentDialogCreateNewSubscene()
        {
            TextBox subsceneName;

            var dialog = new ContentDialog()
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

                _stackPanel.Children.Add(
                    CreateSubsceneTreeView(out SceneEntry subSceneEntry, subsceneName.Text).
                    StackInGrid().
                    WrapInExpanderWithToggleButton(subsceneName.Text).
                    AddContentFlyout(CreateSubRootMenuFlyout(subSceneEntry)));
            }
        }

        private async void ContentDialogRename(TreeEntry entry)
        {
            TextBox fileName;

            var dialog = new ContentDialog()
            {
                XamlRoot = _hierarchy.XamlRoot,
                Title = "Rename",
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = fileName = new TextBox() { Text = entry.Name },
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

                entry.Name = fileName.Text;
                GetEntity(entry.ID).Name = fileName.Text;
            }
        }

        private async void ContentDialogDelete(TreeEntry entry)
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = _hierarchy.XamlRoot,
                Title = "Delete " + entry.Name,
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                entry.Node = null;
                SceneManager.Scene.EntitytManager.Destroy(GetEntity(entry.ID));
            }
        }

        public void PasteEntity(Guid sourceGuid, Guid targetGuid, DataPackageOperation requestedOperation)
        {
            var sourceEntity = GetEntity(sourceGuid);
            var targetEntity = GetEntity(targetGuid);

            if (sourceEntity != null)
                if (requestedOperation == DataPackageOperation.Copy)
                    SceneManager.Scene.EntitytManager.Duplicate(sourceEntity, targetEntity);
                else if (requestedOperation == DataPackageOperation.Move)
                {
                    sourceEntity.Parent = targetEntity;
                    GetTreeEntry(sourceGuid).Node.Parent.Children.Add(GetTreeEntry(targetGuid).Node);
                }
        }
        public void PasteEntity(Guid sourceGuid, SceneEntry targetSceneEntry, DataPackageOperation requestedOperation)
        {
            var sourceEntity = GetEntity(sourceGuid);

            if (sourceEntity != null)
                if (requestedOperation == DataPackageOperation.Copy)
                    SceneManager.Scene.EntitytManager.Duplicate(sourceEntity);
                else if (requestedOperation == DataPackageOperation.Move)
                {
                    SceneManager.Scene.EntitytManager.Duplicate(sourceEntity);
                    SceneManager.Scene.EntitytManager.Destroy(sourceEntity);
                }
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

        private void CopyToClipboard(Guid guid, DataPackageOperation requestedOpertion)
        {
            DataPackage data = new();
            data.SetText(guid.ToString());
            data.RequestedOperation = requestedOpertion;

            Clipboard.SetContent(data);
        }
    }
}
