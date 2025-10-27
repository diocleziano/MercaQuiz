using MercaQuiz.MVVM.Views;

namespace MercaQuiz;

public partial class App : Application
{
    public App(BootstrapPage bootstrap)
    {
        InitializeComponent();
        MainPage = bootstrap;  // pagina di attesa
    }

    //protected override Window CreateWindow(IActivationState? activationState)
    //{
    //    return new Window(new AppShell());
    //}
}