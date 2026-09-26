using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
using tSprite.Common;
using tSprite.Controls;

namespace tSprite.Views;

public class ColorPickerDialog : Window
{
    private readonly ColorWheelControl _wheel;
    private readonly Slider _valSlider;
    private readonly TextBox _hexBox;
    private readonly Border _previewBorder;
    private readonly Border _oldColorBorder;
    private readonly Action<SKColor> _onColorSelected;
    private bool _internalUpdate;
    private SKColor _currentColor;

    public ColorPickerDialog(string title, SKColor initialColor, Action<SKColor> onColorSelected)
    {
        _onColorSelected = onColorSelected;
        _currentColor = initialColor;

        Title = title;
        Width = 360;
        Height = 580;
        MinWidth = 320;
        MinHeight = 450;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = AppFonts.Main;
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));

        RecolorService.RgbToHsv(initialColor, out float initH, out float initS, out float initV);

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*, Auto"),
            Margin = new Thickness(14)
        };

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var contentStack = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var compareRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, 30, *"),
            Margin = new Thickness(0, 0, 0, 4)
        };

        _oldColorBorder = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromRgb(initialColor.Red, initialColor.Green, initialColor.Blue)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60))
        };
        Grid.SetColumn(_oldColorBorder, 0);

        var arrowText = new TextBlock
        {
            Text = "→",
            FontSize = 16,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrowText, 1);

        _previewBorder = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60))
        };
        Grid.SetColumn(_previewBorder, 2);

        compareRow.Children.Add(_oldColorBorder);
        compareRow.Children.Add(arrowText);
        compareRow.Children.Add(_previewBorder);
        contentStack.Children.Add(compareRow);

        _wheel = new ColorWheelControl
        {
            Height = 200,
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Center,
            Hue = initH,
            Saturation = initS,
            Value = initV,
            Margin = new Thickness(0, 4, 0, 4)
        };
        _wheel.ColorPicked += (h, s) => UpdateFromWheel();
        contentStack.Children.Add(_wheel);

        var valHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto")
        };
        var valLabel = new TextBlock
        {
            Text = "Brightness (Value):",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontFamily = AppFonts.Main
        };
        var valText = new TextBlock
        {
            Text = $"{Math.Round(initV * 100)}%",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 100)),
            FontFamily = AppFonts.Main
        };
        Grid.SetColumn(valLabel, 0);
        Grid.SetColumn(valText, 1);
        valHeader.Children.Add(valLabel);
        valHeader.Children.Add(valText);
        contentStack.Children.Add(valHeader);

        _valSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = Math.Round(initV * 100),
            Margin = new Thickness(0, 0, 0, 4)
        };
        _valSlider.ValueChanged += (s, e) =>
        {
            valText.Text = $"{Math.Round(_valSlider.Value)}%";
            _wheel.Value = (float)_valSlider.Value / 100f;
            if (!_internalUpdate)
            {
                UpdateFromWheel();
            }
        };
        contentStack.Children.Add(_valSlider);

        var hexRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *"),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var hexLabel = new TextBlock
        {
            Text = "HEX:",
            FontSize = 13,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            FontFamily = AppFonts.Main
        };
        Grid.SetColumn(hexLabel, 0);

        _hexBox = new TextBox
        {
            Text = $"#{initialColor.Red:X2}{initialColor.Green:X2}{initialColor.Blue:X2}",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4)
        };
        _hexBox.KeyUp += (s, e) => ApplyHex(_hexBox.Text?.Trim() ?? string.Empty);
        Grid.SetColumn(_hexBox, 1);

        hexRow.Children.Add(hexLabel);
        hexRow.Children.Add(_hexBox);
        contentStack.Children.Add(hexRow);

        var presetsPanel = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 4)
        };

        var presets = new[]
        {
            "#000000", "#FFFFFF", "#808080", "#DC143C",
            "#A020F0", "#00E5FF", "#FF6600", "#00FF44",
            "#FF1493", "#FFD700", "#1E90FF", "#8B4513"
        };

        foreach (var hex in presets)
        {
            if (SKColor.TryParse(hex, out var skc))
            {
                var pBtn = new Button
                {
                    Width = 24,
                    Height = 22,
                    Margin = new Thickness(2),
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Color.FromRgb(skc.Red, skc.Green, skc.Blue)),
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(0),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                string targetHex = hex;
                pBtn.Click += (s, e) =>
                {
                    _hexBox.Text = targetHex;
                    ApplyHex(targetHex);
                };
                presetsPanel.Children.Add(pBtn);
            }
        }
        contentStack.Children.Add(presetsPanel);

        scroll.Content = contentStack;
        Grid.SetRow(scroll, 0);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var okBtn = new Button
        {
            Content = "Select",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(45, 80, 130)),
            Foreground = Brushes.White,
            Padding = new Thickness(18, 6),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        okBtn.Click += (s, e) => Confirm();

        var cancelBtn = new Button
        {
            Content = "Cancel",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        cancelBtn.Click += (s, e) => Close();

        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);
        Grid.SetRow(btnRow, 1);

        rootGrid.Children.Add(scroll);
        rootGrid.Children.Add(btnRow);

        Content = rootGrid;

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) Confirm();
            if (e.Key == Key.Escape) Close();
        };

        UpdateFromWheel();
    }

    private void UpdateFromWheel()
    {
        float h = _wheel.Hue;
        float s = _wheel.Saturation;
        float v = (float)_valSlider.Value / 100f;

        _currentColor = RecolorService.HsvToRgb(h, s, v);
        _previewBorder.Background = new SolidColorBrush(Color.FromRgb(_currentColor.Red, _currentColor.Green, _currentColor.Blue));

        if (!_internalUpdate)
        {
            _internalUpdate = true;
            _hexBox.Text = $"#{_currentColor.Red:X2}{_currentColor.Green:X2}{_currentColor.Blue:X2}";
            _internalUpdate = false;
        }
    }

    private void ApplyHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        if (!hex.StartsWith("#")) hex = "#" + hex;

        if (SKColor.TryParse(hex, out var skCol))
        {
            _currentColor = skCol;
            RecolorService.RgbToHsv(skCol, out float h, out float s, out float v);

            _internalUpdate = true;
            _wheel.Hue = h;
            _wheel.Saturation = s;
            _valSlider.Value = Math.Round(v * 100f);
            _wheel.Value = v;
            _previewBorder.Background = new SolidColorBrush(Color.FromRgb(skCol.Red, skCol.Green, skCol.Blue));
            _wheel.InvalidateVisual();
            _internalUpdate = false;
        }
    }

    private void Confirm()
    {
        _onColorSelected(_currentColor);
        Close();
    }
}
