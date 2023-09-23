using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using System;

using Binding = Microsoft.UI.Xaml.Data.Binding;

using Engine.Editor;

namespace Editor.Controller;

internal class BindingHelper
{
    public static void SetBinding(DependencyObject target, DependencyProperty targetProperty, 
        object source, BindingMode mode = BindingMode.OneWay)
    {
        // Create a new instance of the Binding class and set its source, path, and mode properties.
        Binding binding = new Binding
        {
            Source = source,
            Mode = mode
        };

        // Apply the binding to the target property of the target object.
        BindingOperations.SetBinding(target, targetProperty, binding);
    }
}

public class BindableBase : INotifyPropertyChanged
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
public class GeneratePropertyAttribute : Attribute
{
    public GeneratePropertyAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[Generator]
public class PropertyGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required.
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var sourceBuilder = new StringBuilder();

        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

            var fieldsWithAttributes = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Where(f => f.AttributeLists.Count > 0);

            foreach (var field in fieldsWithAttributes)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(field.Declaration.Variables[0]) as IFieldSymbol;

                var generatePropertyAttribute = fieldSymbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == nameof(GeneratePropertyAttribute));

                if (generatePropertyAttribute != null)
                {
                    var propertyName = (string)generatePropertyAttribute.ConstructorArguments[0].Value;
                    var fieldType = fieldSymbol.Type.Name;
                    var fieldName = fieldSymbol.Name;

                    sourceBuilder.AppendLine($@"
                        public {fieldType} {propertyName}
                        {{
                            get => {fieldName};
                            set => SetProperty(ref {fieldName}, value);
                        }}");
                }
            }
        }

        context.AddSource("GeneratedProperties", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }
}
