using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.Views;

namespace TasnimBook.MAUI.ViewModels;

public sealed class BookListViewModel : ObservableObject
{
    private readonly BookCatalogService _catalog;
    private readonly BookListMode _mode;
    private readonly int? _categoryId;
    private readonly AppNavigationService _navigation;
    private readonly List<BooksDTO> _allBooks = [];
    private bool _loaded;
    private bool _isBusy;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private BooksDTO? _selectedBook;
    private CancellationTokenSource? _searchDebounce;

    public string Title { get; }
    public string SearchPlaceholder => _mode == BookListMode.Search
        ? "نام کتاب، نویسنده یا دسته‌بندی…"
        : $"جست‌وجو در {Title}…";
    public string EmptyMessage => _mode == BookListMode.Search && SearchText.Trim().Length < 2
        ? "برای جست‌وجو حداقل دو حرف وارد کنید."
        : string.IsNullOrWhiteSpace(SearchText)
            ? "کتابی برای نمایش نیست."
            : "کتابی مطابق جست‌وجوی شما پیدا نشد.";
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            OnPropertyChanged(nameof(EmptyMessage));
            _searchDebounce?.Cancel();

            if (_mode != BookListMode.Search)
            {
                ApplyLocalFilter();
                return;
            }

            if (value.Trim().Length < 2)
            {
                Books.Clear();
                return;
            }

            _searchDebounce = new CancellationTokenSource();
            _ = SearchAfterDelayAsync(_searchDebounce.Token);
        }
    }
    public BooksDTO? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (!SetProperty(ref _selectedBook, value) || value is null) return;
            OpenBookCommand.Execute(value);
            _selectedBook = null;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<BooksDTO> Books { get; } = [];
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand<BooksDTO> OpenBookCommand { get; }

    public BookListViewModel(
        BookCatalogService catalog,
        string title,
        BookListMode mode,
        AppNavigationService navigation,
        int? categoryId = null)
    {
        _catalog = catalog;
        _mode = mode;
        _navigation = navigation;
        _categoryId = categoryId;
        Title = title;
        SearchCommand = new AsyncCommand(LoadAsync);
        LoadCommand = new AsyncCommand(LoadAsync);
        OpenBookCommand = new AsyncCommand<BooksDTO>(dto =>
            dto is null ? Task.CompletedTask : _navigation.OpenBookAsync(dto));
    }

    public async Task OnAppearingAsync()
    {
        if (!_loaded && _mode != BookListMode.Search) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (IsBusy) return;
        if (_mode == BookListMode.Search && SearchText.Trim().Length < 2)
        {
            Books.Clear();
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<BooksDTO> books = _mode switch
            {
                BookListMode.New => await _catalog.GetNewAsync(80),
                BookListMode.Suggested => await _catalog.GetSuggestedAsync(80),
                BookListMode.Audio => await _catalog.GetAudioAsync(80),
                BookListMode.Category when _categoryId.HasValue => await _catalog.GetByCategoryAsync(_categoryId.Value),
                _ => await _catalog.SearchAsync(SearchText.Trim())
            };

            _allBooks.Clear();
            _allBooks.AddRange(books);
            ApplyLocalFilter();
            _loaded = true;
        }
        catch (Exception exception)
        {
            ErrorMessage = "دریافت کتاب‌ها انجام نشد. اتصال خود را بررسی کنید.";
            System.Diagnostics.Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyLocalFilter()
    {
        var query = SearchText.Trim();
        IEnumerable<BooksDTO> items = _allBooks;
        if (_mode != BookListMode.Search && query.Length > 0)
        {
            items = items.Where(book =>
                Contains(book.Title, query) ||
                Contains(book.AuthorName, query) ||
                Contains(book.CategoryName, query));
        }

        Books.Clear();
        foreach (var book in items) Books.Add(book);
    }

    private async Task SearchAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            await LoadAsync();
        }
        catch (OperationCanceledException) { }
    }

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
}
