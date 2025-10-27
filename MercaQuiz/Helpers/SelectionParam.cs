using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.Helpers;

public class SelectionParam : BindableObject
{
    public static readonly BindableProperty QuestionProperty =
        BindableProperty.Create(nameof(Question), typeof(QuizQuestionItem), typeof(SelectionParam));

    public QuizQuestionItem? Question
    {
        get => (QuizQuestionItem?)GetValue(QuestionProperty);
        set => SetValue(QuestionProperty, value);
    }

    public static readonly BindableProperty OptionProperty =
        BindableProperty.Create(nameof(Option), typeof(QuizOption), typeof(SelectionParam));

    public QuizOption? Option
    {
        get => (QuizOption?)GetValue(OptionProperty);
        set => SetValue(OptionProperty, value);
    }
}
