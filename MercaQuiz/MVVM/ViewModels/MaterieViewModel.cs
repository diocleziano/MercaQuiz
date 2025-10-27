using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MercaQuiz.Data.Repository;
using MercaQuiz.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.MVVM.ViewModels;
public partial class MaterieViewModel : ObservableObject
{
    private readonly MaterieRepository _materieRepo;

    [ObservableProperty]
    private ObservableCollection<Materia> materie = new();

    public MaterieViewModel(MaterieRepository materieRepo)
    {
        _materieRepo = materieRepo;
    }

    [RelayCommand]
    public async Task CaricaAsync()
    {
        var lista = await _materieRepo.GetAllAsync();
        Materie = new ObservableCollection<Materia>(lista.OrderBy(m => m.Nome));
    }

    [RelayCommand]
    public async Task AggiungiAsync()
    {
        await Shell.Current.GoToAsync("materia-edit"); // nuovo
    }

    [RelayCommand]
    public async Task ModificaAsync(Materia? materia)
    {
        if (materia is null) return;
        await Shell.Current.GoToAsync($"materia-edit?materiaId={materia.Id}"); // edit
    }


    //[RelayCommand]
    //public async Task AggiungiAsync()
    //{
    //    var nome = await Shell.Current.DisplayPromptAsync("Nuova materia", "Inserisci il nome:");
    //    if (string.IsNullOrWhiteSpace(nome))
    //        return;

    //    var m = new Materia
    //    {
    //        Nome = nome.Trim(),
    //        CreatoIlUtc = DateTime.UtcNow,
    //        AggiornatoIlUtc = DateTime.UtcNow
    //    };

    //    await _materieRepo.InsertAsync(m);
    //    await CaricaAsync();
    //}

    [RelayCommand]
    public async Task ApriAsync(Materia? materia)
    {
        if (materia is null) return;

        await Shell.Current.GoToAsync($"materia?materiaId={materia.Id}");
    }

    //[RelayCommand]
    //public async Task ModificaAsync(Materia? materia)
    //{
    //    if (materia is null) return;

    //    var nuovoNome = await Shell.Current.DisplayPromptAsync("Modifica materia", "Nome:", initialValue: materia.Nome);
    //    if (string.IsNullOrWhiteSpace(nuovoNome)) return;

    //    materia.Nome = nuovoNome.Trim();
    //    materia.AggiornatoIlUtc = DateTime.UtcNow;
    //    await _materieRepo.UpdateAsync(materia);
    //    await CaricaAsync();
    //}

    [RelayCommand]
    public async Task EliminaAsync(Materia? materia)
    {
        if (materia is null) return;

        var conferma = await Shell.Current.DisplayAlert("Conferma", $"Eliminare \"{materia.Nome}\"?", "Sì", "No");
        if (!conferma) return;

        await _materieRepo.DeleteAsync(materia);
        await CaricaAsync();
    }
}
