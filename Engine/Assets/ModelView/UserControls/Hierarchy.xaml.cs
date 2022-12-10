using Microsoft.UI.Xaml.Controls;
using Editor.Controller;
using Engine.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class Hierarchy : UserControl
    {
        internal Controller.Hierarchy _hierarchyControl;

        public Hierarchy()
        {
            this.InitializeComponent();

            Controller.Main.Instance.Content.Loaded += (s, e) =>
                Controller.Main.Instance.LayoutControl.ViewPort.Loaded += (s, e) =>
                    _hierarchyControl = new Controller.Hierarchy(this, x_StackPanel_Hierarchy);
        }
    }
}
