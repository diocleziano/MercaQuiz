using CommunityToolkit.Maui;
using MercaQuiz.Data.Database;
using MercaQuiz.Data.Repository;
using MercaQuiz.MVVM.ViewModels;
using MercaQuiz.MVVM.Views;
using Microsoft.Extensions.Logging;

namespace MercaQuiz;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Initialize the .NET MAUI Community Toolkit by adding the below line of code
            .UseMauiCommunityToolkit()
            // After initializing the .NET MAUI Community Toolkit, optionally add additional fonts
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Continue initializing your .NET MAUI App here

        // Services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<MaterieRepository>();
        builder.Services.AddSingleton<DomandeQuizRepository>();

        // ViewModel e Views
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<BootstrapPage>();

        builder.Services.AddTransient<MaterieViewModel>();
        builder.Services.AddTransient<HomePage>();

        builder.Services.AddTransient<AddEditMateriaViewModel>();
        builder.Services.AddTransient<AddEditMateriaPage>();

        builder.Services.AddTransient<MateriaDettaglioViewModel>();
        builder.Services.AddTransient<MateriaDettaglioPage>();

        builder.Services.AddTransient<AddEditDomandaViewModel>();
        builder.Services.AddTransient<AddEditDomandaPage>();

        builder.Services.AddTransient<EditDomandaViewModel>();
        builder.Services.AddTransient<EditDomandaPage>();


        builder.Services.AddTransient<QuizViewModel>();
        builder.Services.AddTransient<QuizPage>();

        // Nuove impostazioni
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
