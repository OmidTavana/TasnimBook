using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly BookCatalogService _catalog;
    private bool _isBusy;
    private string? _errorMessage;
    public ObservableCollection<BooksDTO> NewBooks { get; } = [];
    public ObservableCollection<BooksDTO> SuggestedBooks { get; } = [];
    public ObservableCollection<BooksDTO> AudioBooks { get; } = [];
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public AsyncCommand LoadCommand { get; }

    public HomeViewModel(BookCatalogService catalog)
    {
        _catalog = catalog;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsBusy = true; ErrorMessage = null;
        try
        {
            var fresh = _catalog.GetNewAsync(); var suggested = _catalog.GetSuggestedAsync(); var audio = _catalog.GetAudioAsync();
            await Task.WhenAll(fresh, suggested, audio);
            Replace(NewBooks, fresh.Result); Replace(SuggestedBooks, suggested.Result); Replace(AudioBooks, audio.Result);
            if (NewBooks.Count + SuggestedBooks.Count + AudioBooks.Count == 0) ErrorMessage = "کتابی برای نمایش نیست.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private static void Replace(ObservableCollection<BooksDTO> target, IEnumerable<BooksDTO> values)
    {
        target.Clear(); foreach (var value in values.Take(8)) target.Add(value);
    }
}
