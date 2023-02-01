using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System.ComponentModel;

namespace Editor.Controller
{
    internal class BindingHelper
    {
        public static void SetBinding(DependencyObject target, DependencyProperty targetProperty, object source, string sourcePropertyPath, BindingMode mode = BindingMode.OneWay)
        {
            Binding binding = new Binding
            {
                Source = source,
                Path = new PropertyPath(sourcePropertyPath),
                Mode = mode
            };

            BindingOperations.SetBinding(target, targetProperty, binding);
        }
    }

    internal class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
