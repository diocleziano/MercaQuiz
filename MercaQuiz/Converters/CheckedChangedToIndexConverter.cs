using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MercaQuiz.Converters;

public class CheckedChangedToIndexConverter : IValueConverter
{
 public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
 {
 // value is CheckedChangedEventArgs, parameter is the index (int)
 if (value is CheckedChangedEventArgs args && parameter != null && int.TryParse(parameter.ToString(), out var idx))
 {
 return args.Value ? (object)idx : null;
 }
 return null;
 }

 public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
 => throw new NotImplementedException();
}
