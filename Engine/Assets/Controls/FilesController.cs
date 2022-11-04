﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System;
using Microsoft.UI;
using System.IO;
using System.Drawing.Drawing2D;
using CommunityToolkit.WinUI.UI.Controls;
using Windows.Storage;
using Vortice.WinUI;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinRT;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using Vortice.Win32;

namespace Editor.Controls
{
    internal struct Category
    {
        public string Name;
        public Symbol Symbol;
        public string Glyph;
        public bool DefaultColor;
        public string[] SupportedFileTypes;
    }

    internal class FilesController
    {
        public string RootPath { get; private set; }
        public string CurrentProjectTitle { get; private set; }

        public WrapPanel Wrap;
        public Category[] Categories;

        public FilesController(WrapPanel wrap)
        {
            Wrap = wrap;

            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            RootPath = Path.Combine(documentsDir, "3DEngine");
            CurrentProjectTitle = "New Project";

            if (!string.IsNullOrEmpty(CurrentProjectTitle))
                RootPath = Path.Combine(RootPath, CurrentProjectTitle);
        }

        public async void SelectFilesAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { "*" }
            };

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            foreach (StorageFile file in files)
            {
                foreach (var info in Categories)
                    foreach (var type in info.SupportedFileTypes)
                        if (type == file.FileType)
                        {
                            ValidateCategoriesExist();

                            string subFolderPath = Path.Combine(RootPath, info.Name);
                            string destFile = Path.Combine(subFolderPath, file.Name);

                            // To copy a file to another location and
                            // overwrite the destination file if it already exists.
                            System.IO.File.Copy(file.Path, destFile, true);
                        }
            }
        }

        public void Refresh()
        {
            ValidateCategoriesExist();
        }

        public void ValidateCategoriesExist()
        {
            string subFolderPath;

            foreach (var info in Categories)
                if (!Directory.Exists(subFolderPath = Path.Combine(RootPath, info.Name)))
                    Directory.CreateDirectory(subFolderPath);
        }

        public void CreateCatergories(params Category[] categories)
        {
            Categories = categories;

            foreach (var info in Categories)
                if (string.IsNullOrEmpty(info.Glyph))
                    Wrap.Children.Add(CategoryTile(info.Name, info.Symbol, !info.DefaultColor));
                else
                    Wrap.Children.Add(CategoryTile(info.Name, info.Glyph, !info.DefaultColor));
        }

        private Grid CategoryTile(string s, string glyph, bool rndColor = true)
        {
            Grid grid = new Grid();
            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            if (rndColor)
                icon.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(icon);

            return CategoryTile(s, grid, rndColor);
        }

        private Grid CategoryTile(string s, Symbol symbol, bool rndColor = true)
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
