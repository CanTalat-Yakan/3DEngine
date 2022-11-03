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

            Initialize();
        }

        private void Initialize()
        {
            _treeViewController.PopulateTreeView(Tree, SceneControl.ToStringArray(), '/');
        }
    }
}
