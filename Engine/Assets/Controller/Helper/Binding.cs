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
        [Hide]
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

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PropertyAttribute : Attribute
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
