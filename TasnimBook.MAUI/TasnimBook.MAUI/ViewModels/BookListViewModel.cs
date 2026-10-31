using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.ViewModels;

public sealed class BookListViewModel : ObservableObject
{
    private readonly BookCatalogService _catalog;
    private readonly BookListMode _mode;
    private readonly AppNavigationService _navigation;
    private bool _loaded;
    private bool _isBusy;
    private string _searchText = string.Empty;
    private BooksDTO? _selectedBook;
    private CancellationTokenSource? _searchDebounce;

    public string Title { get; }
    public bool IsSearchVisible => _mode == BookListMode.Search;
    public string EmptyMessage => IsSearchVisible ? "عبارت جست‌وجو را وارد کنید." : "کتابی برای نمایش نیست.";
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value) || _mode != BookListMode.Search || value.Trim().Length < 3) return;
            _searchDebounce?.Cancel();
            _searchDebounce = new CancellationTokenSource();
            _ = SearchAfterDelayAsync(_searchDebounce.Token);
        }
    }
    public BooksDTO? SelectedBook
    {
        get => _selectedBook;
        set { if (!SetProperty(ref _selectedBook, value) || value is null) return; OpenBookCommand.Execute(value); _selectedBook = null; OnPropertyChanged(); }
    }
    public ObservableCollection<BooksDTO> Books { get; } = [];
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand<BooksDTO> OpenBookCommand { get; }

    public BookListViewModel(BookCatalogService catalog, string title, BookListMode mode, AppNavigationService navigation)
    {
        _catalog = catalog;
        _mode = mode;
        _navigation = navigation;
        Title = title;
        SearchCommand = new AsyncCommand(LoadAsync);
        OpenBookCommand = new AsyncCommand<BooksDTO>(dto => dto is null ? Task.CompletedTask : _navigation.OpenBookAsync(dto));
    }

    public async Task OnAppearingAsync()
    {
        if (!_loaded && _mode != BookListMode.Search) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            IReadOnlyList<BooksDTO> books = _mode switch
            {
                BookListMode.New => await _catalog.GetNewAsync(40),
                BookListMode.Suggested => await _catalog.GetSuggestedAsync(40),
                BookListMode.Audio => await _catalog.GetAudioAsync(50),
                _ => await _catalog.SearchAsync(SearchText.Trim())
            };
            Books.Clear();
            foreach (var book in books) Books.Add(book);
            _loaded = true;
        }
        finally { IsBusy = false; }
    }

    private async Task SearchAfterDelayAsync(CancellationToken cancellationToken)
    {
        try { await Task.Delay(350, cancellationToken); await LoadAsync(); }
        catch (OperationCanceledException) { }
    }
}
