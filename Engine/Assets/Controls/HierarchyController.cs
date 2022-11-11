using Microsoft.UI.Xaml.Controls;
using System;
using Engine.Utilities;
using Editor.UserControls;

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

            foreach (var entity in engineObjectList)
            {
                var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
                if (entity.Parent != null)
                    newEntry.IDparent = entity.Parent.ID;

                SceneControl.Hierarchy.Add(newEntry);
            }

            foreach (var entity in SceneControl.Hierarchy)
                entity.Node = new TreeViewNode() { Content = entity, IsExpanded = true };

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

        public void SetProperties()
        {
            var content = (TreeEntry)Tree.SelectedNode.Content;

            if (content is null)
                return;

            var id = content.ID;
            var entity = Engine.Core.Instance.Scene.EntitytManager.GetFromID(id);

            PropertiesController.Clear();
            PropertiesController.Set(new Properties(entity));
        }

        private void List_OnAdd()
        {
            var entityList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            var entity = entityList[entityList.Count - 1];
            var newEntry = new TreeEntry() { Name = entity.Name, ID = entity.ID };
            if (entity.Parent != null)
                newEntry.IDparent = entity.Parent.ID;

            newEntry.Node = new TreeViewNode() { Content = newEntry, IsExpanded = true };
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
