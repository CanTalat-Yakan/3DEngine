﻿using Microsoft.UI.Text;
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

namespace Editor.Controls
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
                CreateButton("Create New Entity", (s, e) => Engine.Core.Instance.Scene.EntitytManager.CreateEmpty() )
            };

            SceneTreeView.Tapped += (s, e) => SetProperties();
            SceneTreeView.DragItemsCompleted += (s, e) => SetNewParentTreeEntry((TreeViewNode)e.NewParentItem, (TreeViewNode)e.Items);

            _stackPanel.Children.Add(CreateExpander("Scene", scene));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateButton("Add New Subscene", (s, e) => _stackPanel.Children.Add(CreateExpanderWithToggleButton("Subscene", CreateSubsceneTreeView()))));
            _stackPanel.Children.Add(CreateExpanderWithToggleButton("Subscene", CreateSubsceneTreeView()));
        }

        private Grid[] CreateSubsceneTreeView()
        {
            SubsceneTreeViews = new List<TreeView>();

            TreeView subsceneTreeView;

            var subscene = new Grid[]
            {
                CreateTreeView(out subsceneTreeView),
                CreateButton("Create New Entity", null )
            };

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

    internal partial class HierarchyController
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

        public Entity GetEntity(TreeEntry node)
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            foreach (var entity in engineObjectList)
                if (entity.ID == node.ID)
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

        private Grid CreateTreeView(out TreeView tree)
        {
            Grid grid = new Grid();

            tree = new TreeView() { SelectionMode = TreeViewSelectionMode.Single, HorizontalAlignment = HorizontalAlignment.Stretch };

            grid.Children.Add(tree);

            return grid;
        }

        private Grid CreateSeperator()
        {
            Grid grid = new Grid();

            NavigationViewItemSeparator seperator = new NavigationViewItemSeparator() { Margin = new Thickness(10) };

            grid.Children.Add(seperator);

            return grid;
        }

        private Grid CreateTextFull(string s = "String")
        {
            Grid grid = new Grid();

            TextBlock textInput = new TextBlock() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

            grid.Children.Add(textInput);

            return grid;
        }

        private Grid CreateHeader(string s = "Header")
        {
            Grid grid = new Grid();
            TextBlock header = new TextBlock() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        private Grid CreateSpacer()
        {
            Grid grid = new Grid() { Height = 10 };

            return grid;
        }

        private Grid WrapInExpander(Grid content, string s = "Expander")
        {
            Grid grid = new Grid();
            Expander expander = new Expander() { Header = s, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };

            expander.Content = content;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpander(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.IsExpanded = true;

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpanderWithToggleButton(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.Header = new ToggleButton() { Content = s, IsChecked = true };

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpanderWithEditableHeader(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.Header = new TextBox() { Text = s, Margin = new Thickness(0) };

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateButton(string s, TappedEventHandler tapped)
        {
            Grid grid = new Grid();

            Button button = new Button() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10) };

            button.Tapped += tapped;

            AutoSuggestBox suggestBox = new AutoSuggestBox();

            //FlyoutBase kbase = new FlyoutBase();
            //button.Flyout = suggestBox;

            grid.Children.Add(button);

            return grid;
        }
    }
}
