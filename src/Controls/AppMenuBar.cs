using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using tSprite.Common;
using tSprite.Config;

namespace tSprite.Controls;

public class AppMenuBar : Border
{
    private readonly MenuItem _pluginsMenu;
    public AppMenuBar(
        Action onOpen,
        Action onQuickSave,
        Action onSaveAs,
        Action onExit,
        Action onUndo,
        Action onRedo,
        Action onCopy,
        Action onPaste,
        Action onDuplicate,
        Action onDelete,
        Action onClearSelection,
        Action onCopyClipboard,
        Action onPasteClipboard,
        Action<string> onSelectTool,
        Action onToggleTerminal,
        Action onCustomGrid,
        Action onOpenConfigFolder,
        Action onOpenAssetBrowser,
        Action onRecolor,
        Action onOpenPluginsFolder,
        Action onReloadPlugins)
    {
        Height = 28;
        Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
        BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        BorderThickness = new Thickness(0, 0, 0, 1);

        var menu = new Menu
        {
            FontFamily = AppFonts.Main,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var fileMenu = new MenuItem { Header = "_File" };
        var openItem = new MenuItem { Header = "Open...", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) };
        openItem.Click += (s, e) => onOpen();
        var quickSaveItem = new MenuItem { Header = "Save", InputGesture = new KeyGesture(Key.S, KeyModifiers.Control) };
        quickSaveItem.Click += (s, e) => onQuickSave();
        var saveAsItem = new MenuItem { Header = "Save As...", InputGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift) };
        saveAsItem.Click += (s, e) => onSaveAs();
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => onExit();

        fileMenu.Items.Add(openItem);
        fileMenu.Items.Add(quickSaveItem);
        fileMenu.Items.Add(saveAsItem);
        fileMenu.Items.Add(new Separator());
        fileMenu.Items.Add(exitItem);

        var editMenu = new MenuItem { Header = "_Edit" };
        var undoItem = new MenuItem { Header = "Undo", InputGesture = new KeyGesture(Key.Z, KeyModifiers.Control) };
        undoItem.Click += (s, e) => onUndo();
        var redoItem = new MenuItem { Header = "Redo", InputGesture = new KeyGesture(Key.Y, KeyModifiers.Control) };
        redoItem.Click += (s, e) => onRedo();

        var copyItem = new MenuItem { Header = "Copy (Internal)", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        copyItem.Click += (s, e) => onCopy();
        var pasteItem = new MenuItem { Header = "Paste (Internal)", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) };
        pasteItem.Click += (s, e) => onPaste();
        var duplicateItem = new MenuItem { Header = "Duplicate", InputGesture = new KeyGesture(Key.D, KeyModifiers.Control) };
        duplicateItem.Click += (s, e) => onDuplicate();

        var copyClipItem = new MenuItem { Header = "Copy to Clipboard", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Shift) };
        copyClipItem.Click += (s, e) => onCopyClipboard();
        var pasteClipItem = new MenuItem { Header = "Paste from Clipboard", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control | KeyModifiers.Shift) };
        pasteClipItem.Click += (s, e) => onPasteClipboard();

        var deleteItem = new MenuItem { Header = "Delete", InputGesture = new KeyGesture(Key.Delete) };
        deleteItem.Click += (s, e) => onDelete();
        var deselectItem = new MenuItem { Header = "Deselect", InputGesture = new KeyGesture(Key.Escape) };
        deselectItem.Click += (s, e) => onClearSelection();

        editMenu.Items.Add(undoItem);
        editMenu.Items.Add(redoItem);
        var recolorItem = new MenuItem { Header = "Ramp Recolor...", InputGesture = new KeyGesture(Key.U, KeyModifiers.Control) };
        recolorItem.Click += (s, e) => onRecolor();
        editMenu.Items.Add(recolorItem);
        editMenu.Items.Add(new Separator());
        editMenu.Items.Add(copyItem);
        editMenu.Items.Add(pasteItem);
        editMenu.Items.Add(duplicateItem);
        editMenu.Items.Add(new Separator());
        editMenu.Items.Add(copyClipItem);
        editMenu.Items.Add(pasteClipItem);
        editMenu.Items.Add(new Separator());
        editMenu.Items.Add(deleteItem);
        editMenu.Items.Add(deselectItem);

        var toolsMenu = new MenuItem { Header = "_Tools" };
        var brushItem = new MenuItem { Header = "Brush", InputGesture = new KeyGesture(Key.B) };
        brushItem.Click += (s, e) => onSelectTool("Brush");
        var eraserItem = new MenuItem { Header = "Eraser", InputGesture = new KeyGesture(Key.E) };
        eraserItem.Click += (s, e) => onSelectTool("Eraser");
        var bucketItem = new MenuItem { Header = "Bucket Fill", InputGesture = new KeyGesture(Key.G) };
        bucketItem.Click += (s, e) => onSelectTool("Bucket");
        var selectionItem = new MenuItem { Header = "Marquee Selection", InputGesture = new KeyGesture(Key.M) };
        selectionItem.Click += (s, e) => onSelectTool("Selection");

        toolsMenu.Items.Add(brushItem);
        toolsMenu.Items.Add(eraserItem);
        toolsMenu.Items.Add(bucketItem);
        toolsMenu.Items.Add(selectionItem);

        var viewMenu = new MenuItem { Header = "_View" };
        var terminalItem = new MenuItem { Header = "Toggle Terminal", InputGesture = new KeyGesture(Key.OemTilde) };
        terminalItem.Click += (s, e) => onToggleTerminal();
        var customGridItem = new MenuItem { Header = "Custom Grid..." };
        customGridItem.Click += (s, e) => onCustomGrid();

        viewMenu.Items.Add(terminalItem);
        viewMenu.Items.Add(new Separator());
        viewMenu.Items.Add(customGridItem);
        var assetBrowserItem = new MenuItem { Header = "Asset Browser...", InputGesture = new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Shift) };
        assetBrowserItem.Click += (s, e) => onOpenAssetBrowser();
        viewMenu.Items.Add(new Separator());
        viewMenu.Items.Add(assetBrowserItem);

        var configMenu = new MenuItem { Header = "_Config" };
        var openConfigDirItem = new MenuItem { Header = "Open Config Folder (~/.config/tSprite)" };
        openConfigDirItem.Click += (s, e) => onOpenConfigFolder();
        configMenu.Items.Add(openConfigDirItem);

        var pluginsMenu = new MenuItem { Header = "_Plugins" };
        var openPluginsItem = new MenuItem { Header = "Open Plugins Folder" };
        openPluginsItem.Click += (s, e) => onOpenPluginsFolder();
        var reloadPluginsItem = new MenuItem { Header = "Reload Plugins" };
        reloadPluginsItem.Click += (s, e) => onReloadPlugins();

        pluginsMenu.Items.Add(openPluginsItem);
        pluginsMenu.Items.Add(reloadPluginsItem);
        pluginsMenu.Items.Add(new Separator());
        _pluginsMenu = pluginsMenu;

        menu.Items.Add(fileMenu);
        menu.Items.Add(editMenu);
        menu.Items.Add(toolsMenu);
        menu.Items.Add(viewMenu);
        menu.Items.Add(_pluginsMenu);
        menu.Items.Add(configMenu);

        Child = menu;
    }

    public void AddPluginAction(string category, string name, Action action)
    {
        var item = new MenuItem { Header = name };
        item.Click += (s, e) => action();
        _pluginsMenu.Items.Add(item);
    }
}
