using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using tSprite.Common;

namespace tSprite.Views;

public class CustomGridDialog : Window
{
    private readonly TextBox _widthBox;
    private readonly TextBox _heightBox;

    public int ResultWidth { get; private set; }
    public int ResultHeight { get; private set; }
    public bool IsConfirmed { get; private set; }

    public CustomGridDialog(int currentW, int currentH)
    {
        Title = "Custom Grid";
        Width = 260;
        Height = 175;
        CanResize = true;
        MinWidth = 240;
        MinHeight = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = AppFonts.Main;
        Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));

        ResultWidth = currentW;
        ResultHeight = currentH;

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto, Auto"),
            ColumnDefinitions = new ColumnDefinitions("Auto, *")
        };

        var wLabel = new TextBlock
        {
            Text = "Width:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 8),
            Foreground = Brushes.White
        };
        _widthBox = new TextBox
        {
            Text = currentW.ToString(),
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            CaretBrush = Brushes.White
        };
        Grid.SetRow(wLabel, 0);
        Grid.SetColumn(wLabel, 0);
        Grid.SetRow(_widthBox, 0);
        Grid.SetColumn(_widthBox, 1);

        var hLabel = new TextBlock
        {
            Text = "Height:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Foreground = Brushes.White
        };
        _heightBox = new TextBox
        {
            Text = currentH.ToString(),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            CaretBrush = Brushes.White
        };
        Grid.SetRow(hLabel, 1);
        Grid.SetColumn(hLabel, 0);
        Grid.SetRow(_heightBox, 1);
        Grid.SetColumn(_heightBox, 1);

        grid.Children.Add(wLabel);
        grid.Children.Add(_widthBox);
        grid.Children.Add(hLabel);
        grid.Children.Add(_heightBox);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var applyBtn = new Button
        {
            Content = "Apply",
            Background = new SolidColorBrush(Color.FromRgb(50, 80, 120)),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 4)
        };
        applyBtn.Click += (s, e) => Confirm();

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
            Foreground = Brushes.White,
            Padding = new Thickness(14, 4)
        };
        cancelBtn.Click += (s, e) => Close();

        btnPanel.Children.Add(applyBtn);
        btnPanel.Children.Add(cancelBtn);

        panel.Children.Add(grid);
        panel.Children.Add(btnPanel);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Content = panel };
        Content = scroll;

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) Confirm();
            if (e.Key == Key.Escape) Close();
        };
    }

    private void Confirm()
    {
        if (int.TryParse(_widthBox.Text, out var w) && w > 0 &&
            int.TryParse(_heightBox.Text, out var h) && h > 0)
        {
            ResultWidth = w;
            ResultHeight = h;
            IsConfirmed = true;
            Close();
        }
    }
}
