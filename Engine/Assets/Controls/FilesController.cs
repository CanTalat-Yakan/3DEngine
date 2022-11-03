using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System;
using Microsoft.UI;
using System.Reflection.Emit;

namespace Editor.Controls
{
    internal class FilesController
    {
        public Grid CategoryTile(string s, string glyph, bool rndColor = true)
        {
            Grid grid = new Grid();
            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            if (rndColor)
                icon.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(icon);

            return CategoryTile(s, grid, rndColor);
        }

        public Grid CategoryTile(string s, Symbol symbol, bool rndColor = true)
        {
            Grid grid = new Grid();
            SymbolIcon symbolIcon = new SymbolIcon() { Symbol = symbol };

            if (rndColor)
                symbolIcon.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(symbolIcon);

            return CategoryTile(s, grid, rndColor);
        }

        private Grid CategoryTile(string s, Grid symbol, bool rndColor = true)
        {
            Grid grid = new Grid();
            Button btn = new Button()
            {
                Width = 150,
                Height = 100,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (rndColor)
                btn.Background = new SolidColorBrush(new Color()
                {
                    A = 255,
                    R = (byte)new Random().Next(64, 192),
                    B = (byte)new Random().Next(64, 192),
                    G = (byte)new Random().Next(64, 192)
                });

            StackPanel stack = new StackPanel() { Spacing = 20 };
            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            TextBlock label = new TextBlock() { Text = s };

            if (rndColor)
                label.Foreground = new SolidColorBrush(Colors.White);

            viewbox.Child = symbol;

            stack.Children.Add(viewbox);
            stack.Children.Add(label);

            btn.Content = stack;
            grid.Children.Add(btn);

            return grid;
        }
    }
}
