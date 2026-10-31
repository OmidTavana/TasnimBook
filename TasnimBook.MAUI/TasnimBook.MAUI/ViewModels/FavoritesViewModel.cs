using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class FavoritesViewModel : ObservableObject
{
    private readonly AccountService _account;
    private readonly AppNavigationService _navigation;
    private bool _isBusy;
    private BooksDTO? _selectedBook;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public BooksDTO? SelectedBook
    {
        get => _selectedBook;
        set { if (!SetProperty(ref _selectedBook, value) || value is null) return; OpenBookCommand.Execute(value); _selectedBook = null; OnPropertyChanged(); }
    }
    public ObservableCollection<BooksDTO> Books { get; } = [];
    public AsyncCommand<BooksDTO> OpenBookCommand { get; }

    public FavoritesViewModel(AccountService account, AppNavigationService navigation)
    {
        _account = account;
        _navigation = navigation;
        OpenBookCommand = new AsyncCommand<BooksDTO>(dto => dto is null ? Task.CompletedTask : _navigation.OpenBookAsync(dto));
    }

    public async Task OnAppearingAsync()
    {
        IsBusy = true;
        try
        {
            var books = await _account.GetFavoritesAsync();
            Books.Clear();
            foreach (var book in books) Books.Add(book);
        }
        catch (Exception ex) { await _navigation.AlertAsync("علاقه‌مندی‌ها", ex.Message); }
        finally { IsBusy = false; }
    }
}
