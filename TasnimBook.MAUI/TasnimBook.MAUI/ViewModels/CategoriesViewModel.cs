using System.Collections.ObjectModel;
using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class CategoryCardItem : ObservableObject
{
    private ImageSource? _cover1;
    private ImageSource? _cover2;
    private ImageSource? _cover3;

    public required int Id { get; init; }
    public required string CategoryName { get; init; }
    public int BookCount { get; init; }
    public string BookCountText => BookCount == 0 ? "بدون کتاب" : $"{BookCount} کتاب";
    internal IReadOnlyList<string> CoverUrls { get; init; } = [];
    public ImageSource? Cover1 { get => _cover1; set => SetProperty(ref _cover1, value); }
    public ImageSource? Cover2 { get => _cover2; set => SetProperty(ref _cover2, value); }
    public ImageSource? Cover3 { get => _cover3; set => SetProperty(ref _cover3, value); }
}

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly BookCatalogService _catalog;
    private readonly CategoryCoverCacheService _coverCache;
    private readonly AppNavigationService _navigation;
    private readonly List<CategoryCardItem> _allCategories = [];
    private bool _loaded;
    private bool _isBusy;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private CategoryCardItem? _selectedCategory;

    public ObservableCollection<CategoryCardItem> Categories { get; } = [];
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplyFilter();
            OnPropertyChanged(nameof(EmptyMessage));
        }
    }
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchText)
        ? "هنوز دسته‌بندی‌ای برای نمایش وجود ندارد."
        : "دسته‌بندی‌ای با این نام پیدا نشد.";
    public CategoryCardItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value) || value is null) return;
            OpenCategoryCommand.Execute(value);
            _selectedCategory = null;
            OnPropertyChanged();
        }
    }

    public AsyncCommand LoadCommand { get; }
    public AsyncCommand<CategoryCardItem> OpenCategoryCommand { get; }

    public CategoriesViewModel(
        BookCatalogService catalog,
        CategoryCoverCacheService coverCache,
        AppNavigationService navigation)
    {
        _catalog = catalog;
        _coverCache = coverCache;
        _navigation = navigation;
        LoadCommand = new AsyncCommand(() => LoadAsync(force: true));
        OpenCategoryCommand = new AsyncCommand<CategoryCardItem>(category =>
            category is null ? Task.CompletedTask : _navigation.OpenCategoryBooksAsync(category));
    }

    public async Task OnAppearingAsync()
    {
        if (!_loaded) await LoadAsync(force: false);
    }

    private async Task LoadAsync(bool force)
    {
        if (IsBusy || (_loaded && !force)) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var categoriesTask = _catalog.GetCategoriesAsync();
            var booksTask = _catalog.GetAllAsync();
            await Task.WhenAll(categoriesTask, booksTask);

            var booksByCategory = booksTask.Result
                .GroupBy(book => book.CategoryId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<BooksDTO>)group.ToList());
            var cards = categoriesTask.Result.Select(category =>
                CreateCard(category, booksByCategory.GetValueOrDefault(category.Id) ?? Array.Empty<BooksDTO>()))
                .OrderBy(card => card.CategoryName)
                .ToList();
            _allCategories.Clear();
            _allCategories.AddRange(cards);
            ApplyFilter();
            _loaded = true;
            _ = LoadCoversAsync(cards);
            if (_allCategories.Count == 0)
                ErrorMessage = "دسته‌بندی‌ای از سرور دریافت نشد.";
        }
        catch (Exception exception)
        {
            ErrorMessage = "دریافت دسته‌بندی‌ها انجام نشد. اتصال خود را بررسی کنید.";
            System.Diagnostics.Debug.WriteLine(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private CategoryCardItem CreateCard(BookCat category, IReadOnlyList<BooksDTO> books)
    {
        var covers = books
            .Select(book => book.ImageUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Cast<string>()
            .ToList();

        return new CategoryCardItem
        {
            Id = category.Id,
            CategoryName = category.CategoryName.Trim(),
            BookCount = books.Count,
            CoverUrls = covers
        };
    }

    private async Task LoadCoversAsync(IEnumerable<CategoryCardItem> cards)
    {
        try
        {
            var tasks = cards.Select(async card =>
            {
                var sources = await Task.WhenAll(card.CoverUrls.Select(url => _coverCache.GetAsync(url)));
                var first = sources.ElementAtOrDefault(0);
                var second = sources.ElementAtOrDefault(1) ?? first;
                var third = sources.ElementAtOrDefault(2) ?? second ?? first;
                if (first is not null) card.Cover1 = first;
                if (second is not null) card.Cover2 = second;
                if (third is not null) card.Cover3 = third;
            });
            await Task.WhenAll(tasks);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        IEnumerable<CategoryCardItem> items = _allCategories;
        if (query.Length > 0)
            items = items.Where(category => category.CategoryName.Contains(query, StringComparison.CurrentCultureIgnoreCase));

        Categories.Clear();
        foreach (var category in items) Categories.Add(category);
    }
}
