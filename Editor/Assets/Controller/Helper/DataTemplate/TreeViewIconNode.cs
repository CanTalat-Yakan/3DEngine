using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Editor.Controller;

internal sealed class TreeViewIconNode : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public TreeEntry TreeEntry { get; set; }

    public bool Camera;
    public bool Mesh;

    public bool Folder { get => Children.Count > 0; }

    public int ScriptsCount;
    public bool Scripts
    {
        get
        {
            int i = 1;

            if (Camera) i++;
            if (Mesh) i++;

            // Check the result of "ScriptsCount" with i that considers Transform, Camera and Mesh,
            // and return true when the Count is greater.
            return ScriptsCount > i;
        }
    }

    public string Name { get; set; }
    public bool IsActive { get; set; }
    public float Opacity => IsActive ? 1 : 0.5f;

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

    private void NotifyPropertyChanged(string propertyName) =>
        // Raise the PropertyChanged event with the provided property name argument.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class TreeViewIconNodeTemplateSelector : DataTemplateSelector
{
    public DataTemplate IconNodeTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) { return IconNodeTemplate; }
}
