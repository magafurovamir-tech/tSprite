using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using tSprite.Assets;
using tSprite.Common;

namespace tSprite.Views;

public class AssetBrowserWindow : Window
{
    private readonly TextBox _searchBox;
    private readonly CheckBox _favoritesOnlyCheck;
    private readonly TextBlock _countBlock;
    private readonly ListBox _listBox;
    private readonly Action<string> _onOpen;
    private readonly Action<string> _onImport;

    private AssetCategory? _currentCategory;
    private readonly Button _btnAll;
    private readonly Button _btnItems;
    private readonly Button _btnProjectiles;
    private readonly Button _btnTiles;

    public AssetBrowserWindow(Action<string> onOpen, Action<string> onImport)
    {
        _onOpen = onOpen;
        _onImport = onImport;

        Title = "Asset Browser";
        Width = 560;
        Height = 650;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = AppFonts.Main;
        Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));

        AssetManager.Initialize();

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto, *, Auto"),
            Margin = new Thickness(12)
        };

        var topPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var categoryBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        _btnAll = CreateCategoryButton("All", null);
        _btnItems = CreateCategoryButton("Items", AssetCategory.Items);
        _btnProjectiles = CreateCategoryButton("Projectiles", AssetCategory.Projectiles);
        _btnTiles = CreateCategoryButton("Tiles", AssetCategory.Tiles);

        categoryBar.Children.Add(_btnAll);
        categoryBar.Children.Add(_btnItems);
        categoryBar.Children.Add(_btnProjectiles);
        categoryBar.Children.Add(_btnTiles);

        var searchGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto")
        };

        _searchBox = new TextBox
        {
            Watermark = "Search by name, internal name, or #ID...",
            FontFamily = AppFonts.Main,
            FontSize = 14,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(8, 6)
        };
        _searchBox.KeyUp += (s, e) => ApplyFilter();
        Grid.SetColumn(_searchBox, 0);

        _favoritesOnlyCheck = new CheckBox
        {
            Content = "Favorites ★",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _favoritesOnlyCheck.IsCheckedChanged += (s, e) => ApplyFilter();
        Grid.SetColumn(_favoritesOnlyCheck, 1);

        searchGrid.Children.Add(_searchBox);
        searchGrid.Children.Add(_favoritesOnlyCheck);

        topPanel.Children.Add(categoryBar);
        topPanel.Children.Add(searchGrid);

        _listBox = new ListBox
        {
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            ItemTemplate = new FuncDataTemplate<ItemEntry>((item, _) => CreateItemCard(item))
        };
        Grid.SetRow(_listBox, 1);

        _countBlock = new TextBlock
        {
            FontFamily = AppFonts.Main,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(_countBlock, 2);

        rootGrid.Children.Add(topPanel);
        rootGrid.Children.Add(_listBox);
        rootGrid.Children.Add(_countBlock);

        Content = rootGrid;

        UpdateCategoryButtons();
        ApplyFilter();
    }

    private Button CreateCategoryButton(string label, AssetCategory? category)
    {
        var btn = new Button
        {
            Content = label,
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Padding = new Thickness(14, 5),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        btn.Click += (s, e) =>
        {
            _currentCategory = category;
            UpdateCategoryButtons();
            ApplyFilter();
        };
        return btn;
    }

    private void UpdateCategoryButtons()
    {
        SetButtonStyle(_btnAll, _currentCategory == null);
        SetButtonStyle(_btnItems, _currentCategory == AssetCategory.Items);
        SetButtonStyle(_btnProjectiles, _currentCategory == AssetCategory.Projectiles);
        SetButtonStyle(_btnTiles, _currentCategory == AssetCategory.Tiles);
    }

    private void SetButtonStyle(Button btn, bool isActive)
    {
        btn.Background = isActive
            ? new SolidColorBrush(Color.FromRgb(50, 80, 120))
            : new SolidColorBrush(Color.FromRgb(35, 35, 35));
        btn.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(180, 180, 180));
    }

    private Control CreateItemCard(ItemEntry? item)
    {
        if (item == null) return new Canvas();

        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(0, 2)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("40, *, Auto, Auto, Auto")
        };

        var img = new Image
        {
            Width = 36,
            Height = 36,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 8, 0)
        };
        RenderOptions.SetBitmapInterpolationMode(img, Avalonia.Media.Imaging.BitmapInterpolationMode.None);
        var bmp = AssetManager.GetThumbnail(item);
        if (bmp != null) img.Source = bmp;
        Grid.SetColumn(img, 0);

        var infoPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        var nameText = new TextBlock
        {
            Text = item.Name,
            FontFamily = AppFonts.Main,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };

        var subText = new TextBlock
        {
            Text = item.DisplaySubtitle,
            FontFamily = AppFonts.Main,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150))
        };

        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(subText);
        Grid.SetColumn(infoPanel, 1);

        var favBtn = new Button
        {
            Content = item.IsFavorite ? "★" : "☆",
            Foreground = item.IsFavorite ? new SolidColorBrush(Color.FromRgb(255, 215, 0)) : new SolidColorBrush(Color.FromRgb(140, 140, 140)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 16,
            Padding = new Thickness(6, 4),
            Margin = new Thickness(4, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        favBtn.Click += (s, e) =>
        {
            AssetManager.ToggleFavorite(item);
            favBtn.Content = item.IsFavorite ? "★" : "☆";
            favBtn.Foreground = item.IsFavorite ? new SolidColorBrush(Color.FromRgb(255, 215, 0)) : new SolidColorBrush(Color.FromRgb(140, 140, 140));
            if (_favoritesOnlyCheck.IsChecked == true)
            {
                ApplyFilter();
            }
        };
        Grid.SetColumn(favBtn, 2);

        var importBtn = new Button
        {
            Content = "Import",
            FontFamily = AppFonts.Main,
            FontSize = 12,
            Padding = new Thickness(8, 4),
            Margin = new Thickness(4, 0),
            Background = new SolidColorBrush(Color.FromRgb(40, 65, 95)),
            Foreground = Brushes.White,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsEnabled = item.FilePath != null
        };
        importBtn.Click += (s, e) =>
        {
            if (item.FilePath != null) _onImport(item.FilePath);
        };
        Grid.SetColumn(importBtn, 3);

        var openBtn = new Button
        {
            Content = "Open",
            FontFamily = AppFonts.Main,
            FontSize = 12,
            Padding = new Thickness(8, 4),
            Margin = new Thickness(4, 0),
            Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
            Foreground = Brushes.White,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsEnabled = item.FilePath != null
        };
        openBtn.Click += (s, e) =>
        {
            if (item.FilePath != null) _onOpen(item.FilePath);
        };
        Grid.SetColumn(openBtn, 4);

        grid.Children.Add(img);
        grid.Children.Add(infoPanel);
        grid.Children.Add(favBtn);
        grid.Children.Add(importBtn);
        grid.Children.Add(openBtn);

        card.Child = grid;
        return card;
    }

    private void ApplyFilter()
    {
        var query = _searchBox.Text?.Trim() ?? string.Empty;
        bool favOnly = _favoritesOnlyCheck.IsChecked == true;

        var filtered = new List<ItemEntry>();
        foreach (var item in AssetManager.Items)
        {
            if (_currentCategory.HasValue && item.Category != _currentCategory.Value) continue;
            if (favOnly && !item.IsFavorite) continue;

            if (!string.IsNullOrEmpty(query))
            {
                bool match = item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             item.InternalName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             item.Id.ToString() == query;
                if (!match) continue;
            }

            filtered.Add(item);
        }

        _listBox.ItemsSource = filtered;
        _countBlock.Text = $"{filtered.Count} / {AssetManager.Items.Count} assets";
    }
}
