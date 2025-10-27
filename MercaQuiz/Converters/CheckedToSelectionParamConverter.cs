using MercaQuiz.MVVM.ViewModels;
using System.Globalization;

namespace MercaQuiz.Converters;

public sealed class CheckedToSelectionParamConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = CheckedChangedEventArgs, parameter = RadioButton (passato da XAML)
        if (value is not CheckedChangedEventArgs args) return null;
        if (parameter is not RadioButton rb) return null;

        // Se vuoi reagire solo quando si seleziona (true), scommenta la riga sotto:
        // if (!args.Value) return null;

        // Opzione = BindingContext del RadioButton
        var option = rb.BindingContext as QuizOption;

        // Domanda = BindingContext di un antenato (contenitore del template superiore)
        QuizQuestionItem? question = null;
        Element? cur = rb;
        while (cur != null)
        {
            if (cur.BindingContext is QuizQuestionItem q) { question = q; break; }
            cur = cur.Parent;
        }

        if (question is null || option is null) return null;

        // Ritorniamo un oggetto semplice come parametro
        return new SelectionParam { Question = question, Option = option };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Un piccolo DTO per il parametro del comando
public sealed class SelectionParam
{
    public QuizQuestionItem? Question { get; set; }
    public QuizOption? Option { get; set; }
}
