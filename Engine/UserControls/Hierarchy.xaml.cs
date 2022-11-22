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

        public Hierarchy()
        {
            this.InitializeComponent();

            _hierarchyControl = new HierarchyController(x_TreeView_Hierarchy);

            MainController.Instance.Content.Loaded += (s, e) =>
                MainController.Instance.LayoutControl.ViewPort.Loaded += (s, e) =>
                    _hierarchyControl.PopulateTree();
        }

        private void x_TreeView_Hierarchy_Tapped(object sender, TappedRoutedEventArgs e) { _hierarchyControl.SetProperties(); }

        private void Button_Click(object sender, RoutedEventArgs e) { Engine.Core.Instance.Scene.EntitytManager.CreateEmpty("New Entity"); }
    }
}
