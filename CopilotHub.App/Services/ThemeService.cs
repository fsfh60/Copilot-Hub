using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace CopilotHub.App.Services;

public partial class ThemeService : ObservableObject
{
    [ObservableProperty] private Brush _windowBg = null!;
    [ObservableProperty] private Brush _panelBg = null!;
    [ObservableProperty] private Brush _surfaceBg = null!;
    [ObservableProperty] private Brush _editorBg = null!;
    [ObservableProperty] private Brush _borderBrush = null!;
    [ObservableProperty] private Brush _foreground = null!;
    [ObservableProperty] private Brush _dimForeground = null!;
    [ObservableProperty] private Brush _accentBrush = null!;
    [ObservableProperty] private Brush _terminalFg = null!;
    [ObservableProperty] private Brush _buttonBg = null!;
    [ObservableProperty] private Brush _buttonHoverBg = null!;
    [ObservableProperty] private Brush _inputBg = null!;
    [ObservableProperty] private Brush _inputBorder = null!;
    [ObservableProperty] private bool _isDark;

    public ThemeService()
    {
        DetectAndApply();
    }

    public void DetectAndApply()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var useLightTheme = key?.GetValue("AppsUseLightTheme");
            IsDark = useLightTheme is not int light || light == 0;
        }
        catch
        {
            IsDark = true;
        }

        if (IsDark) ApplyDark(); else ApplyLight();
    }

    private void ApplyDark()
    {
        WindowBg     = Br("#1E1E1E");
        PanelBg      = Br("#2D2D2D");
        SurfaceBg    = Br("#252525");
        EditorBg     = Br("#1A1A1A");
        BorderBrush  = Br("#3C3C3C");
        Foreground   = Br("#D4D4D4");
        DimForeground= Br("#888888");
        AccentBrush  = Br("#4EC9B0");
        TerminalFg   = Br("#00FF00");
        ButtonBg     = Br("#3C3C3C");
        ButtonHoverBg= Br("#505050");
        InputBg      = Br("#2D2D2D");
        InputBorder  = Br("#555555");
    }

    private void ApplyLight()
    {
        WindowBg     = Br("#F3F3F3");
        PanelBg      = Br("#E8E8E8");
        SurfaceBg    = Br("#FFFFFF");
        EditorBg     = Br("#FAFAFA");
        BorderBrush  = Br("#D0D0D0");
        Foreground   = Br("#1E1E1E");
        DimForeground= Br("#666666");
        AccentBrush  = Br("#0078D4");
        TerminalFg   = Br("#0C5E3C");
        ButtonBg     = Br("#DDDDDD");
        ButtonHoverBg= Br("#C8C8C8");
        InputBg      = Br("#FFFFFF");
        InputBorder  = Br("#BBBBBB");
    }

    private static SolidColorBrush Br(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}
