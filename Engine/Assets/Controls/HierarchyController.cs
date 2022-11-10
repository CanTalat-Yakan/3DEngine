using Microsoft.UI.Xaml.Controls;
using System;
using Engine.Utilities;

namespace Editor.Controls
{
    internal class TreeEntry
    {
        public Guid ID;
        public Guid? IDparent;
        public string Name;

        public Entity Entity;
        public TreeViewNode Node;
    }

    internal class HierarchyController
    {
        public TreeView Tree;
        public SceneController SceneControl;

        private TreeViewController _treeViewController = new TreeViewController();

        public HierarchyController(TreeView tree, SceneController scene)
        {
            Tree = tree;
            SceneControl = scene;

            //_treeViewController.PopulateTreeView(Tree, SceneControl.ToStringArray(), '/');
        }

        public void Initialize()
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;
            engineObjectList.EventOnAdd += (s, e) => { List_OnAdd(); };

            foreach (var item in engineObjectList)
            {
                var newEntry = new TreeEntry() { Name = item.Name, ID = item.ID, Entity = item };
                if (item.Parent != null)
                    newEntry.IDparent = item.Parent.ID;

                SceneControl.Hierarchy.Add(newEntry);
            }

            foreach (var item in SceneControl.Hierarchy)
                item.Node = new TreeViewNode() { Content = item.Name, IsExpanded = true };

            TreeEntry tmp;
            foreach (var item in SceneControl.Hierarchy)
                if ((tmp = SceneControl.GetParent(item)) != null)
                {
                    if (!Tree.RootNodes.Contains(tmp.Node))
                        Tree.RootNodes.Add(tmp.Node);

                    if (!tmp.Node.Children.Contains(item.Node))
                        foreach (var child in SceneControl.GetChildren(tmp))
                            tmp.Node.Children.Add(child.Node);
                }
                else if (!Tree.RootNodes.Contains(item.Node))
                    Tree.RootNodes.Add(item.Node);
        }

        private void List_OnAdd()
        {
            var entityList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            var newEntity = entityList[entityList.Count - 1];
            var newEntry = new TreeEntry() { Name = newEntity.Name, ID = newEntity.ID, Entity = newEntity };
            if (newEntity.Parent != null)
                newEntry.IDparent = newEntity.Parent.ID;

            newEntry.Node = new TreeViewNode() { Content = newEntry.Name, IsExpanded = true };
            SceneControl.Hierarchy.Add(newEntry);

            TreeEntry tmp;
            if ((tmp = SceneControl.GetParent(newEntry)) != null)
            {
                if (!Tree.RootNodes.Contains(tmp.Node))
                    Tree.RootNodes.Add(tmp.Node);

                tmp.Node.Children.Add(newEntry.Node);
            }
            else if (!Tree.RootNodes.Contains(newEntry.Node))
                Tree.RootNodes.Add(newEntry.Node);
        }
    }
}
