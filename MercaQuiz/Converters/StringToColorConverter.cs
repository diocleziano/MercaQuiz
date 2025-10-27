using System.Globalization;

namespace MercaQuiz.Converters;
public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                return Color.FromArgb(s); // accetta #RRGGBB o #AARRGGBB
            }
            catch
            {
                // Se fallisce, prova con nomi noti (es. "Red", "Green", "Blue")
                if (Color.TryParse(s, out var c))
                    return c;
            }
        }
        return Colors.Transparent; // default se vuoto o invalido
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
