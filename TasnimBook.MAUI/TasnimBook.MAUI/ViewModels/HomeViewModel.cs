using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly BookCatalogService _catalog;
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private bool _isBusy;
    private string? _errorMessage;
    private BooksDTO? _selectedBook;
    public ObservableCollection<BooksDTO> NewBooks { get; } = [];
    public ObservableCollection<BooksDTO> SuggestedBooks { get; } = [];
    public ObservableCollection<BooksDTO> AudioBooks { get; } = [];
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public BooksDTO? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (!SetProperty(ref _selectedBook, value) || value is null) return;
            OpenBookCommand.Execute(value);
            _selectedBook = null; OnPropertyChanged();
        }
    }
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand<BooksDTO> OpenBookCommand { get; }
    public AsyncCommand NewBooksCommand { get; }
    public AsyncCommand SuggestedBooksCommand { get; }
    public AsyncCommand AudioBooksCommand { get; }
    public AsyncCommand CatalogCommand { get; }
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand LibraryCommand { get; }
    public AsyncCommand AccountCommand { get; }

    public HomeViewModel(BookCatalogService catalog, AccountService account, AppNavigationService navigation)
    {
        _catalog = catalog;
        _account = account;
        _navigation = navigation;
        LoadCommand = new AsyncCommand(LoadAsync);
        OpenBookCommand = new AsyncCommand<BooksDTO>(dto => dto is null ? Task.CompletedTask : _navigation.OpenBookAsync(dto));
        NewBooksCommand = new AsyncCommand(() => _navigation.OpenBookListAsync("کتاب‌های تازه", Views.BookListMode.New));
        SuggestedBooksCommand = new AsyncCommand(() => _navigation.OpenBookListAsync("کتاب‌های پیشنهادی", Views.BookListMode.Suggested));
        AudioBooksCommand = new AsyncCommand(() => _navigation.OpenBookListAsync("کتاب‌های صوتی", Views.BookListMode.Audio));
        CatalogCommand = new AsyncCommand(() => _navigation.OpenBookListAsync("فهرست کتاب‌ها", Views.BookListMode.New));
        SearchCommand = new AsyncCommand(() => _navigation.OpenBookListAsync("جست‌وجوی کتاب", Views.BookListMode.Search));
        LibraryCommand = new AsyncCommand(OpenLibraryAsync);
        AccountCommand = new AsyncCommand(() => _navigation.OpenAccountAsync());
    }

    private async Task OpenLibraryAsync()
    {
        await _account.InitializeAsync();
        if (_account.IsLoggedIn) await _navigation.OpenFavoritesAsync();
        else await _navigation.OpenAccountAsync();
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
