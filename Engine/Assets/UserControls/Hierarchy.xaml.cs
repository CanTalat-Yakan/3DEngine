using Microsoft.UI.Xaml.Controls;
using Editor.Controller;
using Engine.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Hierarchy : UserControl
    {
        internal HierarchyController _hierarchyControl;

        public Hierarchy()
        {
            this.InitializeComponent();

            MainController.Instance.Content.Loaded += (s, e) =>
                MainController.Instance.LayoutControl.ViewPort.Loaded += (s, e) =>
                    _hierarchyControl = new HierarchyController(this, x_StackPanel_Hierarchy);
        }
    }
}
