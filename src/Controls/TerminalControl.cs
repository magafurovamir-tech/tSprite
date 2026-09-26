using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using tSprite.Common;

namespace tSprite.Controls;

public class TerminalControl : Border
{
    private readonly TextBox _inputBox;
    private readonly TextBlock _outputBlock;
    private readonly ScrollViewer _scrollViewer;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public bool IsInputFocused => _inputBox.IsFocused;

    public TerminalControl()
    {
        Height = 180;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        BorderThickness = new Thickness(0, 1, 0, 0);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*, Auto")
        };

        _outputBlock = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontFamily = AppFonts.Main,
            FontSize = 15,
            Margin = new Thickness(10, 8),
            TextWrapping = TextWrapping.Wrap
        };

        _scrollViewer = new ScrollViewer
        {
            Content = _outputBlock
        };
        Grid.SetRow(_scrollViewer, 0);

        var inputGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *"),
            Margin = new Thickness(10, 4, 10, 8)
        };

        var prompt = new TextBlock
        {
            Text = "> ",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 100)),
            FontFamily = AppFonts.Main,
            FontSize = 15,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(prompt, 0);

        _inputBox = new TextBox
        {
            FontFamily = AppFonts.Main,
            FontSize = 15,
            Foreground = Brushes.White,
            CaretBrush = Brushes.White,
            SelectionBrush = new SolidColorBrush(Color.FromArgb(120, 0, 120, 215)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 2, 4, 2),
            MinHeight = 0,
            Height = 24,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _inputBox.Resources["TextControlBackground"] = Brushes.Transparent;
        _inputBox.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
        _inputBox.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
        _inputBox.Resources["TextControlForeground"] = Brushes.White;
        _inputBox.Resources["TextControlForegroundPointerOver"] = Brushes.White;
        _inputBox.Resources["TextControlForegroundFocused"] = Brushes.White;

        Grid.SetColumn(_inputBox, 1);

        _inputBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                var text = _inputBox.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    _history.Add(text);
                    _historyIndex = _history.Count;

                    if (string.IsNullOrEmpty(_outputBlock.Text))
                    {
                        _outputBlock.Text = $"> {text}";
                    }
                    else
                    {
                        _outputBlock.Text += $"\n> {text}";
                    }

                    _inputBox.Text = string.Empty;
                    Dispatcher.UIThread.Post(() => _scrollViewer.ScrollToEnd());
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (_history.Count > 0 && _historyIndex > 0)
                {
                    _historyIndex--;
                    _inputBox.Text = _history[_historyIndex];
                    _inputBox.CaretIndex = _inputBox.Text?.Length ?? 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (_history.Count > 0 && _historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    _inputBox.Text = _history[_historyIndex];
                    _inputBox.CaretIndex = _inputBox.Text?.Length ?? 0;
                }
                else if (_historyIndex == _history.Count - 1)
                {
                    _historyIndex = _history.Count;
                    _inputBox.Text = string.Empty;
                }
                e.Handled = true;
            }
        };

        inputGrid.Children.Add(prompt);
        inputGrid.Children.Add(_inputBox);
        Grid.SetRow(inputGrid, 1);

        grid.Children.Add(_scrollViewer);
        grid.Children.Add(inputGrid);

        Child = grid;
    }

    public void FocusInput()
    {
        _inputBox.Focus();
        _inputBox.CaretIndex = _inputBox.Text?.Length ?? 0;
    }
}
