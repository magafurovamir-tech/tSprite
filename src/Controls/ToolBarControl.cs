using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
using tSprite.Common;
using tSprite.Config;
using tSprite.Views;

namespace tSprite.Controls;

public class ToolBarControl : Border
{
    private readonly Button _btnBrush;
    private readonly Button _btnEraser;
    private readonly Button _btnBucket;
    private readonly Button _btnSelection;
    private readonly Button _btnPrimaryColor;
    private readonly Button _btnSecondaryColor;
    private bool _isColorPickerActive;

    public ToolBarControl(
        Window parentWindow,
        Action<string> onToolSelected,
        Action onOpenRecolor,
        Action onOpenAssetBrowser,
        Action onUndo,
        Action onRedo)
    {
        Width = 44;
        Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 42));
        BorderThickness = new Thickness(0, 0, 1, 0);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Margin = new Thickness(4, 6),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _btnBrush = CreateToolButton("B", "Brush (B)", () => onToolSelected("Brush"));
        _btnEraser = CreateToolButton("E", "Eraser (E)", () => onToolSelected("Eraser"));
        _btnBucket = CreateToolButton("G", "Bucket Fill (G)", () => onToolSelected("Bucket"));
        _btnSelection = CreateToolButton("M", "Marquee Selection (M)", () => onToolSelected("Selection"));

        stack.Children.Add(_btnBrush);
        stack.Children.Add(_btnEraser);
        stack.Children.Add(_btnBucket);
        stack.Children.Add(_btnSelection);

        stack.Children.Add(CreateSeparator());

        _btnPrimaryColor = new Button
        {
            Width = 34,
            Height = 24,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(_btnPrimaryColor, "Primary Color (1) [LMB] - Click to change");
        _btnPrimaryColor.Click += (s, e) =>
        {
            if (_isColorPickerActive) return;
            _isColorPickerActive = true;

            var dlg = new ColorPickerDialog("Primary Color (LMB - Color 1)", SettingsManager.PrimaryColor, (newCol) =>
            {
                SettingsManager.PrimaryColorHex = $"#{newCol.Red:X2}{newCol.Green:X2}{newCol.Blue:X2}";
                SettingsManager.Save();
                UpdateColors();
            });
            dlg.Closed += (sender, args) => _isColorPickerActive = false;
            dlg.Show(parentWindow);
        };

        var swapBtn = new Button
        {
            Content = "⇅",
            Width = 34,
            Height = 18,
            Padding = new Thickness(0),
            FontSize = 13,
            FontFamily = AppFonts.Main,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(swapBtn, "Swap Colors (X)");
        swapBtn.Click += (s, e) =>
        {
            SettingsManager.SwapColors();
            UpdateColors();
        };

        _btnSecondaryColor = new Button
        {
            Width = 34,
            Height = 24,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(_btnSecondaryColor, "Secondary Color (2) [RMB] - Click to change");
        _btnSecondaryColor.Click += (s, e) =>
        {
            if (_isColorPickerActive) return;
            _isColorPickerActive = true;

            var dlg = new ColorPickerDialog("Secondary Color (RMB - Color 2)", SettingsManager.SecondaryColor, (newCol) =>
            {
                SettingsManager.SecondaryColorHex = $"#{newCol.Red:X2}{newCol.Green:X2}{newCol.Blue:X2}";
                SettingsManager.Save();
                UpdateColors();
            });
            dlg.Closed += (sender, args) => _isColorPickerActive = false;
            dlg.Show(parentWindow);
        };

        stack.Children.Add(_btnPrimaryColor);
        stack.Children.Add(swapBtn);
        stack.Children.Add(_btnSecondaryColor);

        stack.Children.Add(CreateSeparator());

        var btnRecolor = CreateIconButton("🎨", "Ramp Recolor (Ctrl+U)", onOpenRecolor);
        var btnAssets = CreateIconButton("📦", "Asset Browser (Ctrl+Shift+A)", onOpenAssetBrowser);
        var btnUndo = CreateIconButton("↩", "Undo (Ctrl+Z)", onUndo);
        var btnRedo = CreateIconButton("↪", "Redo (Ctrl+Y)", onRedo);

        stack.Children.Add(btnRecolor);
        stack.Children.Add(btnAssets);
        stack.Children.Add(btnUndo);
        stack.Children.Add(btnRedo);

        Child = stack;

        UpdateActiveTool(SettingsManager.LastSelectedTool);
        UpdateColors();
    }

    private Button CreateToolButton(string label, string tip, Action onClick)
    {
        var btn = new Button
        {
            Content = label,
            Width = 32,
            Height = 30,
            Padding = new Thickness(0),
            FontSize = 14,
            FontFamily = AppFonts.Main,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(btn, tip);
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private Button CreateIconButton(string icon, string tip, Action onClick)
    {
        var btn = new Button
        {
            Content = icon,
            Width = 32,
            Height = 28,
            Padding = new Thickness(0),
            FontSize = 13,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(btn, tip);
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private Border CreateSeparator()
    {
        return new Border
        {
            Height = 1,
            Width = 28,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            Margin = new Thickness(0, 4)
        };
    }

    public void UpdateActiveTool(string tool)
    {
        SetToolButtonStyle(_btnBrush, tool == "Brush");
        SetToolButtonStyle(_btnEraser, tool == "Eraser");
        SetToolButtonStyle(_btnBucket, tool == "Bucket");
        SetToolButtonStyle(_btnSelection, tool == "Selection");
    }

    private void SetToolButtonStyle(Button btn, bool isActive)
    {
        btn.Background = isActive
            ? new SolidColorBrush(Color.FromRgb(50, 80, 130))
            : Brushes.Transparent;
        btn.BorderBrush = isActive
            ? new SolidColorBrush(Color.FromRgb(100, 150, 220))
            : new SolidColorBrush(Color.FromRgb(50, 50, 50));
    }

    public void UpdateColors()
    {
        var prim = SettingsManager.PrimaryColor;
        var sec = SettingsManager.SecondaryColor;

        _btnPrimaryColor.Background = new SolidColorBrush(Color.FromRgb(prim.Red, prim.Green, prim.Blue));
        _btnSecondaryColor.Background = new SolidColorBrush(Color.FromRgb(sec.Red, sec.Green, sec.Blue));
    }
}
