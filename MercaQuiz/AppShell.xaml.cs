using MercaQuiz.MVVM.Views;

namespace MercaQuiz;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Home
        Routing.RegisterRoute("materia-edit", typeof(AddEditMateriaPage));
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute("materia", typeof(MateriaDettaglioPage));
        Routing.RegisterRoute("domanda-add", typeof(AddEditDomandaPage));
        Routing.RegisterRoute("quiz", typeof(QuizPage));
        Routing.RegisterRoute("domanda-edit", typeof(EditDomandaPage));
    }
}
