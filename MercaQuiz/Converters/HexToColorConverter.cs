using System.Globalization;

namespace MercaQuiz.Converters;
public class HexToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value as string;
        if (string.IsNullOrWhiteSpace(hex))
            return Colors.Gray; // colore di fallback

        try
        {
            // Supporta "#RRGGBB" e "#AARRGGBB"
            return Color.FromArgb(hex.Trim());
        }
        catch
        {
            return Colors.Gray; // fallback su parsing fallito
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
