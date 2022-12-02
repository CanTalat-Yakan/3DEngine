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

    internal partial class HierarchyController
    {
        public TreeView SceneTreeView;
        public List<TreeView> SubsceneTreeViews;

        public List<TreeEntry> Hierarchy;

        private StackPanel _stackPanel;

        private TreeViewNode _itemInvoked = null;

        public HierarchyController(StackPanel stackPanel)
        {
            _stackPanel = stackPanel;

            Hierarchy = new List<TreeEntry>();

            CreateSceneTreeViews();
        }

        private void CreateSceneTreeViews()
        {
            var scene = new Grid[]
            {
                CreateTreeView(out SceneTreeView),
                CreateButton("Create Entity", (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEntity() )
            };
            SceneTreeView.PointerPressed += (s, e) => GetInvokedItemAndSetContextFlyout(s, e);
            SceneTreeView.Tapped += (s, e) => SetProperties();
            SceneTreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewNode)e.NewParentItem, (TreeViewNode)e.Items);

            _stackPanel.Children.Add(scene.StackInGrid().WrapInExpander("Scene").AddContentFlyout(CreateRootMenuFlyout()));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateButton("Add Subscene", (s, e) => _stackPanel.Children.Add(CreateSubsceneTreeView().StackInGrid().WrapInExpanderWithToggleButton("Subscene").AddContentFlyout(CreateSubRootMenuFlyout()))));
            _stackPanel.Children.Add(CreateSubsceneTreeView().StackInGrid().WrapInExpanderWithToggleButton("Subscene").AddContentFlyout(CreateSubRootMenuFlyout()));
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
                _itemInvoked = c.Node;

                ((TreeView)sender).ContextFlyout = CreateDefaultMenuFlyout();
                ((TreeView)sender).ContextFlyout.Opened += (s, e) => ((TreeView)sender).ContextFlyout = null;
            }
        }

        private Grid[] CreateSubsceneTreeView()
        {
            SubsceneTreeViews = new List<TreeView>();

            TreeView subsceneTreeView;

            var subscene = new Grid[]
            {
                CreateTreeView(out subsceneTreeView),
                CreateButton("Create Entity", null )
            };

            subsceneTreeView.ItemInvoked += (s, e) => _itemInvoked = e.InvokedItem as TreeViewNode;
            subsceneTreeView.ContextFlyout = CreateDefaultMenuFlyout();
            subsceneTreeView.Tapped += (s, e) => SetProperties();
            subsceneTreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewNode)e.NewParentItem, (TreeViewNode)e.Items);

            SubsceneTreeViews.Add(subsceneTreeView);

            return subscene;
        }

        public void DeselectTreeViewNodes()
        {
            SceneTreeView.SelectedNode = null;

            foreach (var subsceneTreeView in SubsceneTreeViews)
                subsceneTreeView.SelectedNode = null;
        }

        public void PopulateTree()
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;
            engineObjectList.OnAddEvent += (s, e) => OnAdd();

            foreach (var entity in engineObjectList)
            {
                var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
                if (entity.Parent != null)
                    newEntry.IDparent = entity.Parent.ID;

                Hierarchy.Add(newEntry);
            }

            foreach (var entry in Hierarchy)
                entry.Node = new TreeViewNode() { Content = entry, IsExpanded = true };

            TreeEntry parent;
            foreach (var entry in Hierarchy)
                if ((parent = GetParent(entry)) is null)
                    SceneTreeView.RootNodes.Add(entry.Node);
                else
                    parent.Node.Children.Add(entry.Node);
        }

        public void SetProperties()
        {
            if (SceneTreeView.SelectedNode is null)
                return;

            var treeEntry = (TreeEntry)SceneTreeView.SelectedNode.Content;
            var entity = Engine.Core.Instance.Scene.EntitytManager.GetFromID(treeEntry.ID);

            PropertiesController.Clear();
            PropertiesController.Set(new Properties(entity));
        }

        private void OnAdd()
        {
            var entityList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            var entity = entityList[^1];
            var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
            if (entity.Parent != null)
                newEntry.IDparent = entity.Parent.ID;

            newEntry.Node = new TreeViewNode() { Content = newEntry, IsExpanded = true };
            Hierarchy.Add(newEntry);

            TreeEntry parent;
            if ((parent = GetParent(newEntry)) is null)
                SceneTreeView.RootNodes.Add(newEntry.Node);
            else
                parent.Node.Children.Add(newEntry.Node);
        }
    }

    internal partial class HierarchyController : HelperController
    {
        public TreeEntry GetParent(TreeEntry node)
        {
            if (node.IDparent != null)
                foreach (var item in Hierarchy)
                    if (item.ID == node.IDparent.Value)
                        return item;

            return null;
        }

        public TreeEntry[] GetChildren(TreeEntry node)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == node.ID)
                        list.Add(item);

            return list.ToArray();
        }

        public Entity GetEntity(TreeEntry entry)
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            foreach (var entity in engineObjectList)
                if (entity.ID == entry.ID)
                    return entity;

            return null;
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

            //items[0].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Move);
            //items[1].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Copy);
            //items[2].Click += (s, e) => PasteFileSystemEntryFromClipboard(path);

            //items[3].Click += (s, e) => ContentDialogRename(path);
            //items[4].Click += (s, e) => ContentDialogDelete(path);

            items[5].Click += (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEntity(GetEntity(_itemInvoked.Parent.Content as TreeEntry));
            items[6].Click += (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEntity(GetEntity(_itemInvoked.Content as TreeEntry));

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

        private MenuFlyout CreateRootMenuFlyout(string path = "")
        {
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };

            //items[0].Click += (s, e) => OpenFolder(path);
            //items[1].Click += (s, e) => OpenFolder(path);

            //items[2].Click += (s, e) => ContentDialogRename(path);
            //items[3].Click += (s, e) => ContentDialogDelete(path);

            items[2].Click += (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEntity();

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

        private MenuFlyout CreateSubRootMenuFlyout(string path = "")
        {
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Save", Icon = new SymbolIcon(Symbol.Save) },
                new MenuFlyoutItem() { Text = "Show in Files", Icon = new SymbolIcon(Symbol.Document) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Unload" },
                new MenuFlyoutItem() { Text = "Load" },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Create Entity" },
            };

            //items[0].Click += (s, e) => OpenFolder(path);
            //items[1].Click += (s, e) => OpenFolder(path);

            //items[2].Click += (s, e) => ContentDialogRename(path);
            //items[3].Click += (s, e) => ContentDialogDelete(path);

            items[4].Click += (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEntity();

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                menuFlyout.Items.Add(item);

                if (item.Text == "Show in Files"
                    || item.Text == "Delete"
                    || item.Text == "Load")
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
    }
}
