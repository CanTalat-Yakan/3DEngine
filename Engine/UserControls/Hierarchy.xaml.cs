using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Editor.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Hierarchy : UserControl
    {
        private HierarchyController _hierarchyControl;
        private SceneController _sceneControl;

        public Hierarchy()
        {
            this.InitializeComponent();

            _sceneControl = new SceneController();
            _hierarchyControl = new HierarchyController(x_TreeView_Hierarchy, _sceneControl);

            MainController.Instance.Content.Loaded += (s, e) =>
                MainController.Instance.LayoutControl.ViewPort.Loaded += (s, e) =>
                    _hierarchyControl.Initialize();
        }

        private void x_TreeView_Hierarchy_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _hierarchyControl.SetProperties();
        }
    }
}
