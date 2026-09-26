using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
using tSprite.Common;

namespace tSprite.Views;

public class RecolorDialog : Window
{
    private readonly Slider _targetHueSlider;
    private readonly Slider _sourceHueSlider;
    private readonly Slider _toleranceSlider;
    private readonly Slider _minSatSlider;
    private readonly Slider _minValSlider;
    private readonly Slider _satMultSlider;
    private readonly CheckBox _shadowShiftCheck;
    private readonly TextBox _sourceHexBox;
    private readonly TextBox _targetHexBox;
    private readonly Border _sourceColorPreview;
    private readonly Border _targetColorPreview;
    private bool _isCommitted;
    private bool _internalUpdate;

    private readonly Action<float, float, float, float, float, float, bool> _onPreview;
    private readonly Action<float, float, float, float, float, float, bool> _onCommit;
    private readonly Action _onCancel;

    public RecolorDialog(
        float initialSourceHue,
        Action<float, float, float, float, float, float, bool> onPreview,
        Action<float, float, float, float, float, float, bool> onCommit,
        Action onCancel)
    {
        _onPreview = onPreview;
        _onCommit = onCommit;
        _onCancel = onCancel;

        Title = "Ramp Recolor";
        Width = 360;
        Height = 540;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = AppFonts.Main;
        Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*, Auto"),
            Margin = new Thickness(14)
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var contentStack = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var previewRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, 30, *"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        _sourceColorPreview = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60))
        };
        Grid.SetColumn(_sourceColorPreview, 0);

        var arrowText = new TextBlock
        {
            Text = "→",
            FontSize = 16,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrowText, 1);

        _targetColorPreview = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60))
        };
        Grid.SetColumn(_targetColorPreview, 2);

        previewRow.Children.Add(_sourceColorPreview);
        previewRow.Children.Add(arrowText);
        previewRow.Children.Add(_targetColorPreview);
        contentStack.Children.Add(previewRow);

        var hexInputsRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, 30, *"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var srcSk = RecolorService.HsvToRgb(initialSourceHue, 1f, 1f);
        _sourceHexBox = new TextBox
        {
            Text = $"#{srcSk.Red:X2}{srcSk.Green:X2}{srcSk.Blue:X2}",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4)
        };
        _sourceHexBox.KeyUp += (s, e) => ApplySourceHex(_sourceHexBox.Text?.Trim() ?? string.Empty);
        Grid.SetColumn(_sourceHexBox, 0);

        _targetHexBox = new TextBox
        {
            Text = "#A020F0",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            CaretBrush = Brushes.White,
            Padding = new Thickness(6, 4)
        };
        _targetHexBox.KeyUp += (s, e) => ApplyTargetHex(_targetHexBox.Text?.Trim() ?? string.Empty);
        Grid.SetColumn(_targetHexBox, 2);

        hexInputsRow.Children.Add(_sourceHexBox);
        hexInputsRow.Children.Add(_targetHexBox);
        contentStack.Children.Add(hexInputsRow);

        var presetsPanel = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 8)
        };

        var presetColors = new[]
        {
            ("Purple", "#A020F0"),
            ("Cyan", "#00E5FF"),
            ("Orange", "#FF6600"),
            ("Green", "#00FF44"),
            ("Pink", "#FF1493"),
            ("Gold", "#FFD700"),
            ("Blue", "#1E90FF"),
            ("Crimson", "#DC143C")
        };

        foreach (var (_, hex) in presetColors)
        {
            if (SKColor.TryParse(hex, out var skc))
            {
                var pBtn = new Button
                {
                    Width = 26,
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
                    _targetHexBox.Text = targetHex;
                    ApplyTargetHex(targetHex);
                };
                presetsPanel.Children.Add(pBtn);
            }
        }
        contentStack.Children.Add(presetsPanel);

        _targetHueSlider = CreateSliderRow(contentStack, "Target Hue:", 0, 360, 285, "°");
        _sourceHueSlider = CreateSliderRow(contentStack, "Source Hue:", 0, 360, initialSourceHue, "°");
        _toleranceSlider = CreateSliderRow(contentStack, "Hue Tolerance:", 5, 180, 40, "°");
        _minSatSlider = CreateSliderRow(contentStack, "Min Saturation (Protect Metal):", 0, 1, 0.20, "");
        _minValSlider = CreateSliderRow(contentStack, "Min Brightness (Protect Outlines):", 0, 1, 0.15, "");
        _satMultSlider = CreateSliderRow(contentStack, "Saturation Multiplier:", 0.5, 2.0, 1.0, "x");

        _shadowShiftCheck = new CheckBox
        {
            Content = "Cold Shadow Shift (-10° Hue for dark pixels)",
            IsChecked = true,
            Foreground = Brushes.White,
            FontFamily = AppFonts.Main,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 8)
        };
        _shadowShiftCheck.IsCheckedChanged += (s, e) => TriggerPreview();
        contentStack.Children.Add(_shadowShiftCheck);

        scrollViewer.Content = contentStack;
        Grid.SetRow(scrollViewer, 0);

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var applyBtn = new Button
        {
            Content = "Apply",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(45, 80, 130)),
            Foreground = Brushes.White,
            Padding = new Thickness(18, 6),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        applyBtn.Click += (s, e) => Apply();

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
        cancelBtn.Click += (s, e) => Cancel();

        btnRow.Children.Add(applyBtn);
        btnRow.Children.Add(cancelBtn);
        Grid.SetRow(btnRow, 1);

        rootGrid.Children.Add(scrollViewer);
        rootGrid.Children.Add(btnRow);

        Content = rootGrid;

        Closed += (s, e) =>
        {
            if (!_isCommitted)
            {
                _onCancel();
            }
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) Apply();
            if (e.Key == Key.Escape) Cancel();
        };

        UpdateColorSwatches();
        TriggerPreview();
    }

    private void ApplySourceHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (SKColor.TryParse(hex, out var skCol))
        {
            RecolorService.RgbToHsv(skCol, out float h, out _, out _);
            _internalUpdate = true;
            _sourceHueSlider.Value = h;
            _internalUpdate = false;
            UpdateColorSwatches();
            TriggerPreview();
        }
    }

    private void ApplyTargetHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (SKColor.TryParse(hex, out var skCol))
        {
            RecolorService.RgbToHsv(skCol, out float h, out _, out _);
            _internalUpdate = true;
            _targetHueSlider.Value = h;
            _internalUpdate = false;
            UpdateColorSwatches();
            TriggerPreview();
        }
    }

    private Slider CreateSliderRow(StackPanel parent, string label, double min, double max, double value, string unit)
    {
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto")
        };

        var labelBlock = new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontFamily = AppFonts.Main
        };

        var valBlock = new TextBlock
        {
            Text = unit == "°" ? $"{Math.Round(value)}°" : $"{value:0.00}{unit}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 100)),
            FontFamily = AppFonts.Main
        };

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(valBlock, 1);
        headerGrid.Children.Add(labelBlock);
        headerGrid.Children.Add(valBlock);
        parent.Children.Add(headerGrid);

        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            Margin = new Thickness(0, 0, 0, 4)
        };

        slider.ValueChanged += (s, e) =>
        {
            valBlock.Text = unit == "°" ? $"{Math.Round(slider.Value)}°" : $"{slider.Value:0.00}{unit}";

            if (!_internalUpdate)
            {
                if (slider == _targetHueSlider)
                {
                    var curSk = RecolorService.HsvToRgb((float)slider.Value, 1f, 1f);
                    _targetHexBox.Text = $"#{curSk.Red:X2}{curSk.Green:X2}{curSk.Blue:X2}";
                }
                else if (slider == _sourceHueSlider)
                {
                    var curSk = RecolorService.HsvToRgb((float)slider.Value, 1f, 1f);
                    _sourceHexBox.Text = $"#{curSk.Red:X2}{curSk.Green:X2}{curSk.Blue:X2}";
                }
            }

            UpdateColorSwatches();
            TriggerPreview();
        };

        parent.Children.Add(slider);
        return slider;
    }

    private void UpdateColorSwatches()
    {
        var srcSk = RecolorService.HsvToRgb((float)_sourceHueSlider.Value, 1f, 1f);
        var tgtSk = RecolorService.HsvToRgb((float)_targetHueSlider.Value, 1f, 1f);

        _sourceColorPreview.Background = new SolidColorBrush(Color.FromRgb(srcSk.Red, srcSk.Green, srcSk.Blue));
        _targetColorPreview.Background = new SolidColorBrush(Color.FromRgb(tgtSk.Red, tgtSk.Green, tgtSk.Blue));
    }

    private void TriggerPreview()
    {
        _onPreview(
            (float)_sourceHueSlider.Value,
            (float)_toleranceSlider.Value,
            (float)_targetHueSlider.Value,
            (float)_minSatSlider.Value,
            (float)_minValSlider.Value,
            (float)_satMultSlider.Value,
            _shadowShiftCheck.IsChecked == true
        );
    }

    private void Apply()
    {
        _isCommitted = true;
        _onCommit(
            (float)_sourceHueSlider.Value,
            (float)_toleranceSlider.Value,
            (float)_targetHueSlider.Value,
            (float)_minSatSlider.Value,
            (float)_minValSlider.Value,
            (float)_satMultSlider.Value,
            _shadowShiftCheck.IsChecked == true
        );
        Close();
    }

    private void Cancel()
    {
        _onCancel();
        Close();
    }
}
