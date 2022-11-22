using Microsoft.UI.Xaml.Controls;
using System;
using Engine.Utilities;
using Editor.UserControls;
using System.Collections.Generic;

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

    internal class HierarchyController
    {
        public TreeView Tree;

        public List<TreeEntry> Hierarchy;


        public HierarchyController(TreeView tree)
        {
            Tree = tree;

            Hierarchy = new List<TreeEntry>();
        }

        public void PopulateTree()
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;
            engineObjectList.OnAddEvent += (s, e) => { List_OnAdd(); };

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
                    Tree.RootNodes.Add(entry.Node);
                else
                    parent.Node.Children.Add(entry.Node);
        }

        public void SetProperties()
        {
            if (Tree.SelectedNode is null)
                return;

            var treeEntry = (TreeEntry)Tree.SelectedNode.Content;
            var entity = Engine.Core.Instance.Scene.EntitytManager.GetFromID(treeEntry.ID);

            PropertiesController.Clear();
            PropertiesController.Set(new Properties(entity));
        }

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

        private void List_OnAdd()
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
                Tree.RootNodes.Add(newEntry.Node);
            else
                parent.Node.Children.Add(newEntry.Node);
        }
    }
}
