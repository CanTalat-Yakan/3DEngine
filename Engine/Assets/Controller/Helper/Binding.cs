using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Reflection;
using System;
using Engine.Editor;

namespace Editor.Controller
{
    internal class BindingHelper
    {
        public static void SetBinding(DependencyObject target, DependencyProperty targetProperty, object source, string sourcePropertyPath, BindingMode mode = BindingMode.OneWay)
        {
            // Create a new instance of the Binding class and set its source, path, and mode properties.
            Binding binding = new Binding
            {
                Source = source,
                Path = new PropertyPath(sourcePropertyPath),
                Mode = mode
            };

            // Apply the binding to the target property of the target object.
            BindingOperations.SetBinding(target, targetProperty, binding);
        }
    }

    internal class BindableBase : INotifyPropertyChanged
    {
        [Hide]
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            // Check if the new value to be set is equal to the current value of the property.
            if (Equals(storage, value))
                return false;

            // Set the new value for the property.
            storage = value;
            // Raise the PropertyChanged event, notifying that the value of the property has changed.
            RaisePropertyChanged(propertyName);

            // Return true to indicate that the value has been successfully changed.
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) =>
            // Raise the PropertyChanged event with the provided property name argument.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    internal class PropertyAttribute : Attribute
    {
        public PropertyAttribute(string name)
        {
            Name = name;

            //PropertyGenerator.GenerateProperties();
        }

        public string Name { get; }
    }

    internal static class PropertyGenerator
    {
        public static void GenerateProperties(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var propertyAttribute = field.GetCustomAttribute<PropertyAttribute>();
                if (propertyAttribute == null)
                    continue;

                GenerateProperty(type, field, propertyAttribute.Name);
            }
        }

        private static void GenerateProperty(Type type, FieldInfo field, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property != null)
                return;

            var getMethod = new DynamicMethod("Get" + propertyName, field.FieldType, new[] { type }, type);
            var getIL = getMethod.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, field);
            getIL.Emit(OpCodes.Ret);

            var setMethod = new DynamicMethod("Set" + propertyName, typeof(void), new[] { type, field.FieldType }, type);
            var setIL = setMethod.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, field);
            setIL.Emit(OpCodes.Ret);

            //var propertyBuilder = type.DefineProperty(propertyName, PropertyAttributes.None, field.FieldType, Type.EmptyTypes);
            //propertyBuilder.SetGetMethod(getMethod);
            //propertyBuilder.SetSetMethod(setMethod);
        }
    }
}
