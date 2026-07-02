using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Budget.Models;
using Microsoft.Win32;

namespace Budget.Services;

public static class ThemeManager
{
    private static readonly ThemeBrushes Light = new(
        AppBackground: Color.FromRgb(244, 247, 251),
        Surface: Color.FromRgb(255, 255, 255),
        SurfaceAlt: Color.FromRgb(248, 250, 253),
        BorderSoft: Color.FromRgb(227, 233, 242),
        TextPrimary: Color.FromRgb(18, 32, 51),
        TextSecondary: Color.FromRgb(96, 112, 139),
        Accent: Color.FromRgb(59, 130, 246),
        AccentDark: Color.FromRgb(37, 99, 235),
        AccentSoft: Color.FromRgb(219, 234, 254),
        AccentForeground: Color.FromRgb(255, 255, 255),
        AccentInteractive: Color.FromRgb(37, 99, 235),
        AccentInteractiveHover: Color.FromRgb(29, 78, 216),
        Success: Color.FromRgb(22, 163, 74),
        Warning: Color.FromRgb(245, 158, 11),
        Danger: Color.FromRgb(153, 27, 27),
        ToolbarBackground: Color.FromRgb(226, 232, 240),
        ToolbarHover: Color.FromRgb(215, 224, 234),
        DangerBackground: Color.FromRgb(254, 226, 226),
        DangerHover: Color.FromRgb(254, 202, 202),
        SummaryBackground: Color.FromRgb(249, 251, 254),
        StatusBackground: Color.FromRgb(248, 250, 253),
        CardShadow: Color.FromArgb(26, 15, 23, 42));

    private static readonly ThemeBrushes Dark = new(
        AppBackground: Color.FromRgb(15, 23, 42),
        Surface: Color.FromRgb(30, 41, 59),
        SurfaceAlt: Color.FromRgb(15, 23, 42),
        BorderSoft: Color.FromRgb(51, 65, 85),
        TextPrimary: Color.FromRgb(226, 232, 240),
        TextSecondary: Color.FromRgb(148, 163, 184),
        Accent: Color.FromRgb(96, 165, 250),
        AccentDark: Color.FromRgb(59, 130, 246),
        AccentSoft: Color.FromRgb(30, 58, 138),
        AccentForeground: Color.FromRgb(15, 23, 42),
        AccentInteractive: Color.FromRgb(96, 165, 250),
        AccentInteractiveHover: Color.FromRgb(147, 197, 253),
        Success: Color.FromRgb(74, 222, 128),
        Warning: Color.FromRgb(251, 191, 36),
        Danger: Color.FromRgb(248, 113, 113),
        ToolbarBackground: Color.FromRgb(51, 65, 85),
        ToolbarHover: Color.FromRgb(71, 85, 105),
        DangerBackground: Color.FromRgb(69, 10, 10),
        DangerHover: Color.FromRgb(96, 23, 23),
        SummaryBackground: Color.FromRgb(17, 24, 39),
        StatusBackground: Color.FromRgb(15, 23, 42),
        CardShadow: Color.FromArgb(110, 0, 0, 0));

    public static ThemeMode CurrentThemeMode { get; private set; } = ThemeMode.System;

    public static ThemeMode ResolveEffectiveTheme(ThemeMode requestedMode)
    {
        return requestedMode == ThemeMode.System ? GetSystemTheme() : requestedMode;
    }

    public static void ApplyTheme(ThemeMode requestedMode)
    {
        CurrentThemeMode = requestedMode;
        var effectiveMode = ResolveEffectiveTheme(requestedMode);
        var palette = effectiveMode == ThemeMode.Dark ? Dark : Light;

        ApplyBrush("AppBackgroundBrush", palette.AppBackground);
        ApplyBrush("SurfaceBrush", palette.Surface);
        ApplyBrush("SurfaceAltBrush", palette.SurfaceAlt);
        ApplyBrush("BorderBrushSoft", palette.BorderSoft);
        ApplyBrush("TextPrimaryBrush", palette.TextPrimary);
        ApplyBrush("TextSecondaryBrush", palette.TextSecondary);
        ApplyBrush("AccentBrush", palette.Accent);
        ApplyBrush("AccentBrushDark", palette.AccentDark);
        ApplyBrush("AccentSoftBrush", palette.AccentSoft);
        ApplyBrush("AccentForegroundBrush", palette.AccentForeground);
        ApplyBrush("AccentInteractiveBrush", palette.AccentInteractive);
        ApplyBrush("AccentInteractiveHoverBrush", palette.AccentInteractiveHover);
        ApplyBrush("SuccessBrush", palette.Success);
        ApplyBrush("WarningBrush", palette.Warning);
        ApplyBrush("DangerBrush", palette.Danger);
        ApplyBrush("ToolbarBackgroundBrush", palette.ToolbarBackground);
        ApplyBrush("ToolbarHoverBrush", palette.ToolbarHover);
        ApplyBrush("DangerBackgroundBrush", palette.DangerBackground);
        ApplyBrush("DangerHoverBrush", palette.DangerHover);
        ApplyBrush("SummaryBackgroundBrush", palette.SummaryBackground);
        ApplyBrush("StatusBackgroundBrush", palette.StatusBackground);
        Application.Current.Resources["CardShadowColor"] = palette.CardShadow;
        ThemeApplied?.Invoke(null, EventArgs.Empty);
    }

    public static event EventHandler? ThemeApplied;

    public static event EventHandler? SystemThemeChanged;

    public static void RaiseSystemThemeChanged()
    {
        SystemThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static ThemeMode GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
        }
        catch
        {
            // ignore and fall through to light
        }

        return ThemeMode.Light;
    }

    private static void ApplyBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Application.Current.Resources[key] = brush;
    }

    private sealed record ThemeBrushes(
        Color AppBackground,
        Color Surface,
        Color SurfaceAlt,
        Color BorderSoft,
        Color TextPrimary,
        Color TextSecondary,
        Color Accent,
        Color AccentDark,
        Color AccentSoft,
        Color AccentForeground,
        Color AccentInteractive,
        Color AccentInteractiveHover,
        Color Success,
        Color Warning,
        Color Danger,
        Color ToolbarBackground,
        Color ToolbarHover,
        Color DangerBackground,
        Color DangerHover,
        Color SummaryBackground,
        Color StatusBackground,
        Color CardShadow);
}


