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
        private HierarchyController hierarchyControl;
        private SceneController sceneControl;

        public Hierarchy()
        {
            this.InitializeComponent();

            sceneControl = new SceneController();
            hierarchyControl = new HierarchyController(x_TreeView_Hierarchy, sceneControl);

            MainController.Instance.Content.Loaded += (s, e) =>
                MainController.Instance.LayoutControl.ViewPort.Loaded += (s, e) =>
                    hierarchyControl.Initialize();
        }
    }
}
