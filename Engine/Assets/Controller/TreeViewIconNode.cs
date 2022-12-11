using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Editor.Controller
{
    internal class TreeViewIconNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public TreeEntry TreeEntry { get; set; }

        public Visibility Camera;
        public Visibility Mesh;

        public Visibility Folder { get => Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }

        public int ScriptsCount;
        public Visibility Scripts
        {
            get
            {
                int i = 1;

                if (Camera == Visibility.Visible) i++;
                if (Mesh == Visibility.Visible) i++;

                return ScriptsCount > i ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string Name { get; set; }
        public bool IsActive { get; set; }
        public float Opacity { get => IsActive ? 1 : 0.5f; }


        private ObservableCollection<TreeViewIconNode> _children;
        public ObservableCollection<TreeViewIconNode> Children
        {
            get
            {
                if (_children == null)
                    _children = new ObservableCollection<TreeViewIconNode>();

                return _children;
            }
            set { _children = value; }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    class TreeViewIconNodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate IconNodeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) { return IconNodeTemplate; }
    }

}
