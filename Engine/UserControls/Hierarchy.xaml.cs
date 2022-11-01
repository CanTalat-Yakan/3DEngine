using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Editor.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Hierarchy : UserControl
    {
        TreeView treeView;

        public Hierarchy()
        {
            this.InitializeComponent();
            treeView = x_TreeView_Hierarchy;

            MainController.Singleton.m_Content.Loaded += Initialize0;
        }


        void Initialize0(object sender, RoutedEventArgs e)
        {
            MainController.Singleton.m_Layout.m_ViewPort.Loaded += Initialize;
        }

        void Initialize(object sender, RoutedEventArgs e)
        {
            var engineObjectList = MainController.Singleton.m_Layout.m_ViewPort.engineLoop.scene.entitytManager.list;
            engineObjectList.OnAdd += list_OnAdd;

            SceneController scene = new SceneController();
            foreach (var item in engineObjectList)
            {
                var newEntry = new TreeEntry() { Name = item.name, ID = item.id }; // Object = item,
                if (item.parent != null)
                    newEntry.IDparent = item.parent.id;

                scene.m_Hierarchy.Add(newEntry);
            }

            foreach (var item in scene.m_Hierarchy)
                item.Node = new TreeViewNode() { Content = item.Name, IsExpanded = true };

            TreeEntry tmp;
            foreach (var item in scene.m_Hierarchy)
                if ((tmp = scene.GetParent(item)) != null)
                {
                    if (!x_TreeView_Hierarchy.RootNodes.Contains(tmp.Node))
                        x_TreeView_Hierarchy.RootNodes.Add(tmp.Node);

                    if (!tmp.Node.Children.Contains(item.Node))
                        foreach (var child in scene.GetChildren(tmp))
                            tmp.Node.Children.Add(child.Node);
                }
                else if (!x_TreeView_Hierarchy.RootNodes.Contains(item.Node))
                    x_TreeView_Hierarchy.RootNodes.Add(item.Node);
        }

        void list_OnAdd(object sender, EventArgs e)
        {
            var engineObjectList = MainController.Singleton.m_Layout.m_ViewPort.engineLoop.scene.entitytManager.list;

            SceneController scene = new SceneController();

            var newObject = engineObjectList[engineObjectList.Count - 1];
            var newEntry = new TreeEntry() { Name = newObject.name, ID = newObject.id }; // Object = newObject
            if (newObject.parent != null)
                newEntry.IDparent = newObject.parent.id;

            newEntry.Node = new TreeViewNode() { Content = newEntry.Name, IsExpanded = true };
            scene.m_Hierarchy.Add(newEntry);

            TreeEntry tmp;
            if ((tmp = scene.GetParent(newEntry)) != null)
            {
                if (!x_TreeView_Hierarchy.RootNodes.Contains(tmp.Node))
                    x_TreeView_Hierarchy.RootNodes.Add(tmp.Node);

                tmp.Node.Children.Add(newEntry.Node);
            }
            else if (!x_TreeView_Hierarchy.RootNodes.Contains(newEntry.Node))
                x_TreeView_Hierarchy.RootNodes.Add(newEntry.Node);
        }
    }
}
