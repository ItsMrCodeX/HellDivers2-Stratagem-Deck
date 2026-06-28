using System.Globalization;

namespace StratagemDeck.Mobile.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Color.FromArgb("#3FB950");
        return Color.FromArgb("#F85149");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Dictionary<string, Color> CategoryColors = new()
    {
        ["Offensive: Orbital"] = Color.FromArgb("#30363D"),
        ["Offensive: Eagle"] = Color.FromArgb("#30363D"),
        ["Supply: Support Weapons"] = Color.FromArgb("#21262D"),
        ["Supply: Backpacks"] = Color.FromArgb("#21262D"),
        ["Supply: Vehicles"] = Color.FromArgb("#21262D"),
        ["Defensive"] = Color.FromArgb("#1C2128"),
        ["Mission"] = Color.FromArgb("#21262D"),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string cat && CategoryColors.TryGetValue(cat, out var color))
            return color;
        return Color.FromArgb("#1C2128");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SlotSelectedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is string idxStr && int.TryParse(idxStr, out int idx))
            return selected == idx ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#21262D");
        return Color.FromArgb("#21262D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
