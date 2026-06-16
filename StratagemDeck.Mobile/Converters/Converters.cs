using System.Globalization;

namespace HD2Companion.Mobile.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Colors.LimeGreen;
        return Colors.Red;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Dictionary<string, Color> CategoryColors = new()
    {
        ["Offensive: Orbital"] = Color.FromArgb("#E94560"),
        ["Offensive: Eagle"] = Color.FromArgb("#E94560"),
        ["Supply: Support Weapons"] = Color.FromArgb("#0F3460"),
        ["Supply: Backpacks"] = Color.FromArgb("#16213E"),
        ["Supply: Vehicles"] = Color.FromArgb("#16213E"),
        ["Defensive"] = Color.FromArgb("#533483"),
        ["Mission"] = Color.FromArgb("#3B82F6"),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string cat && CategoryColors.TryGetValue(cat, out var color))
            return color;
        return Color.FromArgb("#0F3460");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SlotSelectedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is string idxStr && int.TryParse(idxStr, out int idx))
            return selected == idx ? Color.FromArgb("#E94560") : Color.FromArgb("#0F3460");
        return Color.FromArgb("#0F3460");
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
