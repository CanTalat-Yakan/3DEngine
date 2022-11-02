using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Editor.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Hierarchy : UserControl
    {
        private TreeView _treeView;

        public Hierarchy()
        {
            this.InitializeComponent();

            _treeView = x_TreeView_Hierarchy;

            MainController.Instance.Content.Loaded += Initialize0;
        }

        private void Initialize0(object sender, RoutedEventArgs e)
        {
            MainController.Instance.LayoutControl.ViewPort.Loaded += Initialize;
        }

        private void Initialize(object sender, RoutedEventArgs e)
        {
            var engineObjectList = Engine.Core.Instance.Scene.EntitytManager.EntityList;
            engineObjectList.EventOnAdd += list_OnAdd;

            SceneController scene = new SceneController();
            foreach (var item in engineObjectList)
            {
                var newEntry = new TreeEntry() { Name = item.Name, ID = item.ID }; // Object = item,
                if (item.Parent != null)
                    newEntry.IDparent = item.Parent.ID;

                scene.m_Hierarchy.Add(newEntry);
            }

            foreach (var item in scene.m_Hierarchy)
                item.Node = new TreeViewNode() { Content = item.Name, IsExpanded = true };

            TreeEntry tmp;
            foreach (var item in scene.m_Hierarchy)
                if ((tmp = scene.GetParent(item)) != null)
                {
                    if (!_treeView.RootNodes.Contains(tmp.Node))
                        _treeView.RootNodes.Add(tmp.Node);

                    if (!tmp.Node.Children.Contains(item.Node))
                        foreach (var child in scene.GetChildren(tmp))
                            tmp.Node.Children.Add(child.Node);
                }
                else if (!_treeView.RootNodes.Contains(item.Node))
                    _treeView.RootNodes.Add(item.Node);
        }

        private void list_OnAdd(object sender, EventArgs e)
        {
            var entityList = Engine.Core.Instance.Scene.EntitytManager.EntityList;

            SceneController scene = new SceneController();

            var newObject = entityList[entityList.Count - 1];
            var newEntry = new TreeEntry() { Name = newObject.Name, ID = newObject.ID }; // Object = newObject
            if (newObject.Parent != null)
                newEntry.IDparent = newObject.Parent.ID;

            newEntry.Node = new TreeViewNode() { Content = newEntry.Name, IsExpanded = true };
            scene.m_Hierarchy.Add(newEntry);

            TreeEntry tmp;
            if ((tmp = scene.GetParent(newEntry)) != null)
            {
                if (!_treeView.RootNodes.Contains(tmp.Node))
                    _treeView.RootNodes.Add(tmp.Node);

                tmp.Node.Children.Add(newEntry.Node);
            }
            else if (!_treeView.RootNodes.Contains(newEntry.Node))
                _treeView.RootNodes.Add(newEntry.Node);
        }
    }
}
