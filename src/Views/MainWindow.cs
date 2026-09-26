using tSprite.Plugins;
using System.Diagnostics;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;
using tSprite.Common;
using tSprite.Config;
using tSprite.Controls;

namespace tSprite.Views;

public class MainWindow : Window
{
    private readonly CanvasControl _canvasControl;
    private readonly TerminalControl _terminalControl;
    private readonly ToolBarControl _toolBarControl;
    private readonly TextBlock _statusCoordsText;
    private readonly Button _frameButton;
    private readonly TextBlock _statusCanvasText;
    private readonly AppMenuBar _menuBar;
    private readonly TSpritePluginContext _pluginContext;
    private string? _currentFilePath;

    public MainWindow()
    {
        Title = "tSprite";
        Width = ConfigManager.WindowWidth;
        Height = ConfigManager.WindowHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        MinWidth = 600;
        MinHeight = 450;
        FontFamily = AppFonts.Main;

        _canvasControl = new CanvasControl();
        _terminalControl = new TerminalControl
        {
            IsVisible = false
        };

        var statusBar = new Border
        {
            Height = 26,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 0)
        };

        var statusPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _statusCoordsText = new TextBlock
        {
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var sep1 = new TextBlock
        {
            Text = "|",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _frameButton = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 0),
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 100)),
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _frameButton.Click += (s, e) =>
        {
            var menu = CreateFrameMenu();
            menu.Open(_frameButton);
        };

        var sep2 = new TextBlock
        {
            Text = "|",
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _statusCanvasText = new TextBlock
        {
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        statusPanel.Children.Add(_statusCoordsText);
        statusPanel.Children.Add(sep1);
        statusPanel.Children.Add(_frameButton);
        statusPanel.Children.Add(sep2);
        statusPanel.Children.Add(_statusCanvasText);

        statusBar.Child = statusPanel;

        _pluginContext = new TSpritePluginContext(new CanvasPluginWrapper(_canvasControl));

        _menuBar = new AppMenuBar(
            onOpen: () => _ = OpenFileAsync(),
            onQuickSave: () => _ = SaveFileAsync(false),
            onSaveAs: () => _ = SaveFileAsync(true),
            onExit: () => Close(),
            onUndo: () => _canvasControl.Undo(),
            onRedo: () => _canvasControl.Redo(),
            onCopy: () => _canvasControl.CopyInternal(),
            onPaste: () => _canvasControl.PasteInternal(),
            onDuplicate: () => _canvasControl.DuplicateInternal(),
            onDelete: () => _canvasControl.DeleteSelection(),
            onClearSelection: () => _canvasControl.ClearSelection(),
            onCopyClipboard: () => _ = CopyToClipboardAsync(),
            onPasteClipboard: () => _ = PasteFromClipboardAsync(),
            onSelectTool: (t) => { SettingsManager.SetTool(t); _canvasControl.UpdateKeyModifiers(KeyModifiers.None); _toolBarControl?.UpdateActiveTool(t); },
            onToggleTerminal: () => ToggleTerminal(),
            onCustomGrid: () => _ = ShowCustomGridDialogAsync(),
            onOpenConfigFolder: () => {
                var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "tSprite");
                if (Directory.Exists(p)) Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true });
            },
            onOpenAssetBrowser: () => OpenAssetBrowserWindow(),
            onRecolor: () => OpenRecolorDialog(),
            onOpenPluginsFolder: () => {
                var p = PluginLoader.GetPluginsDirectory();
                if (Directory.Exists(p)) Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true });
            },
            onReloadPlugins: () => ReloadPlugins()
        );

        _pluginContext.ActionRegistered += (act) => _menuBar.AddPluginAction(act.Category, act.Name, act.Action);
        PluginLoader.LoadAll(_pluginContext);

        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto, *, Auto, Auto")
        };

        _toolBarControl = new ToolBarControl(
            this,
            onToolSelected: (t) => { SettingsManager.SetTool(t); _canvasControl.UpdateKeyModifiers(KeyModifiers.None); _toolBarControl?.UpdateActiveTool(t); _toolBarControl?.UpdateActiveTool(t); },
            onOpenRecolor: () => OpenRecolorDialog(),
            onOpenAssetBrowser: () => OpenAssetBrowserWindow(),
            onUndo: () => _canvasControl.Undo(),
            onRedo: () => _canvasControl.Redo()
        );

        var centerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, *")
        };
        Grid.SetColumn(_toolBarControl, 0);
        Grid.SetColumn(_canvasControl, 1);
        centerGrid.Children.Add(_toolBarControl);
        centerGrid.Children.Add(_canvasControl);

        Grid.SetRow(_menuBar, 0);
        Grid.SetRow(centerGrid, 1);
        Grid.SetRow(_terminalControl, 2);
        Grid.SetRow(statusBar, 3);

        mainGrid.Children.Add(_menuBar);
        mainGrid.Children.Add(centerGrid);
        mainGrid.Children.Add(_terminalControl);
        mainGrid.Children.Add(statusBar);

        Content = mainGrid;

        _canvasControl.StatusChanged += UpdateStatusBar;
        UpdateStatusBar();

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OpenRecolorDialog()
    {
        _canvasControl.BeginRecolor();
        float dominantHue = _canvasControl.GetDominantTargetHue();
        var dlg = new RecolorDialog(
            dominantHue,
            onPreview: (srcH, tol, tgtH, minS, minV, satMult, shadowShift) => _canvasControl.PreviewRecolor(srcH, tol, tgtH, minS, minV, satMult, shadowShift),
            onCommit: (srcH, tol, tgtH, minS, minV, satMult, shadowShift) => _canvasControl.CommitRecolor(srcH, tol, tgtH, minS, minV, satMult, shadowShift),
            onCancel: () => _canvasControl.CancelRecolor()
        );
        dlg.ShowDialog(this);
    }

    private void OpenAssetBrowserWindow()
    {
        var win = new AssetBrowserWindow(
            onOpen: (filePath) =>
            {
                if (File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    using var bitmap = SKBitmap.Decode(stream);
                    if (bitmap != null)
                    {
                        _canvasControl.LoadNewImage(bitmap);
                        _pluginContext.SetCanvas(new CanvasPluginWrapper(_canvasControl));
                        _currentFilePath = filePath;
                        ConfigManager.RecentFile = filePath;
                        ConfigManager.LastSavedFileName = Path.GetFileName(filePath);
                        ConfigManager.Save();
                    }
                }
            },
            onImport: (filePath) =>
            {
                if (File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    using var bitmap = SKBitmap.Decode(stream);
                    if (bitmap != null)
                    {
                        _canvasControl.PasteBitmap(bitmap);
                    }
                }
            }
        );
        win.Show(this);
    }

    private async Task ShowCustomGridDialogAsync()
    {
        var dlg = new CustomGridDialog(
            _canvasControl.Spritesheet.FrameWidth,
            _canvasControl.Spritesheet.FrameHeight
        );
        await dlg.ShowDialog(this);

        if (dlg.IsConfirmed)
        {
            _canvasControl.SetGridMode(dlg.ResultWidth, dlg.ResultHeight);
        }
    }

    private ContextMenu CreateFrameMenu()
    {
        var menu = new ContextMenu();

        var autoItem = new MenuItem { Header = "Auto-detect" };
        autoItem.Click += (s, e) => _canvasControl.AutoDetectSpritesheet();
        menu.Items.Add(autoItem);

        var islandItem = new MenuItem { Header = "Sprite Islands" };
        islandItem.Click += (s, e) => _canvasControl.SetIslandMode();
        menu.Items.Add(islandItem);

        var customItem = new MenuItem { Header = "Custom Grid Size..." };
        customItem.Click += async (s, e) => await ShowCustomGridDialogAsync();
        menu.Items.Add(customItem);

        menu.Items.Add(new Separator());

        int gw = _canvasControl.GridWidth;
        int gh = _canvasControl.GridHeight;

        if (gh > gw && gh % gw == 0)
        {
            var vertItem = new MenuItem { Header = $"Vertical Strip ({gw}x{gw})" };
            vertItem.Click += (s, e) => _canvasControl.SetGridMode(gw, gw);
            menu.Items.Add(vertItem);
        }

        int[] presets = { 16, 18, 20, 24, 32, 48, 64 };
        foreach (var sz in presets)
        {
            if (gw >= sz && gh >= sz)
            {
                var item = new MenuItem { Header = $"{sz}x{sz} Grid" };
                int sVal = sz;
                item.Click += (s, e) => _canvasControl.SetGridMode(sVal, sVal);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new Separator());

        var offItem = new MenuItem { Header = "Single Sprite" };
        offItem.Click += (s, e) => _canvasControl.DisableSpritesheet();
        menu.Items.Add(offItem);

        return menu;
    }

    private void UpdateStatusBar()
    {
        var coords = _canvasControl.HoveredX.HasValue && _canvasControl.HoveredY.HasValue
            ? $"{_canvasControl.HoveredX.Value}, {_canvasControl.HoveredY.Value}"
            : "--, --";

        _statusCoordsText.Text = $"[ {coords} ]";

        var sheet = _canvasControl.Spritesheet;

        if (sheet.Mode == SpritesheetMode.Islands)
        {
            int total = sheet.DetectedIslands.Count;

            if (_canvasControl.HoveredX.HasValue && _canvasControl.HoveredY.HasValue)
            {
                int hx = _canvasControl.HoveredX.Value;
                int hy = _canvasControl.HoveredY.Value;
                int foundIdx = -1;

                for (int i = 0; i < total; i++)
                {
                    if (sheet.DetectedIslands[i].Contains(hx, hy))
                    {
                        foundIdx = i;
                        break;
                    }
                }

                if (foundIdx >= 0)
                {
                    var isl = sheet.DetectedIslands[foundIdx];
                    _frameButton.Content = $"Sprite: {foundIdx + 1}/{total} [{isl.Width}x{isl.Height}] ▾";
                }
                else
                {
                    _frameButton.Content = $"Sprite: -/{total} [Islands] ▾";
                }
            }
            else
            {
                _frameButton.Content = $"Sprite: -/{total} [Islands] ▾";
            }
        }
        else if (sheet.Mode == SpritesheetMode.Grid)
        {
            int fw = sheet.FrameWidth;
            int fh = sheet.FrameHeight;
            int totalActive = _canvasControl.ActiveGridFramesCount;

            if (_canvasControl.HoveredX.HasValue && _canvasControl.HoveredY.HasValue)
            {
                int col = _canvasControl.HoveredX.Value / fw;
                int row = _canvasControl.HoveredY.Value / fh;

                var activeIdx = _canvasControl.GetActiveFrameIndex(col, row);

                if (activeIdx.HasValue)
                {
                    _frameButton.Content = $"Frame: {activeIdx.Value}/{totalActive} [{fw}x{fh}] ▾";
                }
                else
                {
                    _frameButton.Content = $"Frame: [Empty] [{fw}x{fh}] ▾";
                }
            }
            else
            {
                _frameButton.Content = $"Frame: -/{totalActive} [{fw}x{fh}] ▾";
            }
        }
        else
        {
            _frameButton.Content = "Single Sprite ▾";
        }

        int zoomPercent = (int)Math.Round(_canvasControl.Zoom * 100);

        if (_canvasControl.Selection.HasValue)
        {
            var sel = _canvasControl.Selection.Value;
            _statusCanvasText.Text = $"Sel: {sel.Width}x{sel.Height}  |  Canvas: {_canvasControl.GridWidth}x{_canvasControl.GridHeight}  |  Zoom: {zoomPercent}%";
        }
        else
        {
            _statusCanvasText.Text = $"Canvas: {_canvasControl.GridWidth}x{_canvasControl.GridHeight}  |  Zoom: {zoomPercent}%";
        }
    }

    private async Task OpenFileAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.png", "*.bmp", "*.jpg", "*.jpeg", "*.webp" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            _currentFilePath = files[0].Path.LocalPath;
            ConfigManager.RecentFile = _currentFilePath;
            ConfigManager.Save();

            await using var stream = await files[0].OpenReadAsync();
            using var bitmap = SKBitmap.Decode(stream);
            if (bitmap != null)
            {
                _canvasControl.LoadNewImage(bitmap);
                _pluginContext.SetCanvas(new CanvasPluginWrapper(_canvasControl));
            }
        }
    }

    private async Task SaveFileAsync(bool forceSaveAs = false)
    {
        if (!forceSaveAs && !string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
        {
            using var stream = File.Create(_currentFilePath);
            _canvasControl.ExportToPng(stream, new SKRectI(0, 0, _canvasControl.GridWidth, _canvasControl.GridHeight));
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        bool hasSelection = _canvasControl.Selection.HasValue;
        var hoveredFrame = _canvasControl.GetHoveredFrameRect();

        string suggestedName = !string.IsNullOrEmpty(ConfigManager.LastSavedFileName)
            ? ConfigManager.LastSavedFileName
            : (hasSelection ? "selection.png" : (hoveredFrame.HasValue ? "frame.png" : "sprite.png"));
        string title = hasSelection ? "Save Selection As PNG" : (hoveredFrame.HasValue ? "Save Frame As PNG" : "Save As PNG");

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = "png",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image")
                {
                    Patterns = new[] { "*.png" }
                }
            }
        });

        if (file != null)
        {
            _currentFilePath = file.Path.LocalPath;
            ConfigManager.RecentFile = _currentFilePath;
            ConfigManager.LastSavedFileName = Path.GetFileName(_currentFilePath);
            ConfigManager.Save();

            await using var stream = await file.OpenWriteAsync();
            var targetRect = hasSelection ? _canvasControl.Selection : (hoveredFrame.HasValue ? hoveredFrame : null);
            _canvasControl.ExportToPng(stream, targetRect);
        }
    }

    private async Task CopyToClipboardAsync()
    {
        await ClipboardService.CopyAsync(GetTopLevel(this), _canvasControl);
    }

    private async Task PasteFromClipboardAsync()
    {
        await ClipboardService.PasteAsync(GetTopLevel(this), _canvasControl);
    }

    private async Task DuplicateClipboardAsync()
    {
        await CopyToClipboardAsync();
        await PasteFromClipboardAsync();
    }

    private void ReloadPlugins()
    {
        _pluginContext.SetCanvas(new CanvasPluginWrapper(_canvasControl));
        PluginLoader.LoadAll(_pluginContext);
    }

    private void ToggleTerminal()
    {
        _terminalControl.IsVisible = !_terminalControl.IsVisible;
        if (_terminalControl.IsVisible)
        {
            Dispatcher.UIThread.Post(() => _terminalControl.FocusInput());
        }
        else
        {
            _canvasControl.Focus();
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (KeymapManager.Matches("ToggleTerminal", e))
        {
            ToggleTerminal();
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _canvasControl.UpdateKeyModifiers(e.KeyModifiers);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _canvasControl.UpdateKeyModifiers(e.KeyModifiers);

        if (_terminalControl.IsInputFocused)
        {
            return;
        }

        if (KeymapManager.Matches("Recolor", e) || KeymapManager.Matches("RecolorAlt", e))
        {
            OpenRecolorDialog();
            return;
        }
        if (KeymapManager.Matches("AssetBrowser", e))
        {
            OpenAssetBrowserWindow();
            return;
        }
        if (KeymapManager.Matches("QuickSave", e))
        {
            _ = SaveFileAsync(false);
            return;
        }
        if (KeymapManager.Matches("SaveAs", e))
        {
            _ = SaveFileAsync(true);
            return;
        }
        if (KeymapManager.Matches("OpenFile", e))
        {
            _ = OpenFileAsync();
            return;
        }
        if (KeymapManager.Matches("CopyClipboard", e))
        {
            _ = CopyToClipboardAsync();
            return;
        }
        if (KeymapManager.Matches("PasteClipboard", e))
        {
            _ = PasteFromClipboardAsync();
            return;
        }
        if (KeymapManager.Matches("DuplicateClipboard", e))
        {
            _ = DuplicateClipboardAsync();
            return;
        }
        if (KeymapManager.Matches("Copy", e))
        {
            _canvasControl.CopyInternal();
            return;
        }
        if (KeymapManager.Matches("Paste", e))
        {
            _canvasControl.PasteInternal();
            return;
        }
        if (KeymapManager.Matches("Duplicate", e))
        {
            _canvasControl.DuplicateInternal();
            return;
        }
        if (KeymapManager.Matches("Undo", e))
        {
            _canvasControl.Undo();
            return;
        }
        if (KeymapManager.Matches("Redo", e) || KeymapManager.Matches("RedoAlt", e))
        {
            _canvasControl.Redo();
            return;
        }
        if (KeymapManager.Matches("Delete", e))
        {
            _canvasControl.DeleteSelection();
            return;
        }
        if (KeymapManager.Matches("Escape", e))
        {
            _canvasControl.ClearSelection();
            return;
        }
        if (KeymapManager.Matches("Brush", e))
        {
            SettingsManager.SetTool("Brush");
            _canvasControl.UpdateKeyModifiers(e.KeyModifiers);
            return;
        }
        if (KeymapManager.Matches("Eraser", e))
        {
            SettingsManager.SetTool("Eraser");
            _canvasControl.UpdateKeyModifiers(e.KeyModifiers);
            return;
        }
        if (KeymapManager.Matches("Bucket", e))
        {
            SettingsManager.SetTool("Bucket");
            _canvasControl.UpdateKeyModifiers(e.KeyModifiers);
            return;
        }
        if (KeymapManager.Matches("Selection", e))
        {
            SettingsManager.SetTool("Selection");
            _canvasControl.UpdateKeyModifiers(e.KeyModifiers);
            return;
        }
    }
}
