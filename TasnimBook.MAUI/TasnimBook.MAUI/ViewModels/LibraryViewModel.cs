using System.Collections.ObjectModel;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class LibraryViewModel(AccountService account, AppNavigationService navigation) : ObservableObject
{
    private bool _isBusy;
    public ObservableCollection<BooksDTO> Books { get; } = [];
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public AsyncCommand<BooksDTO> OpenCommand { get; } = new(dto => dto is null ? Task.CompletedTask : navigation.OpenBookAsync(dto));
    public AsyncCommand<BooksDTO> RemoveCommand { get; } = new(async dto =>
    {
        if (dto is null || !await navigation.ConfirmAsync("حذف از کتابخانه", $"«{dto.Title}» از کتابخانه شخصی حذف شود؟ فایل دانلود و پیشرفت مطالعه حفظ می‌شود.", "حذف", "انصراف")) return;
        await account.RemoveFromLibraryAsync(dto.Id);
    });

    public async Task OnAppearingAsync()
    {
        IsBusy = true;
        try
        {
            Books.Clear();
            foreach (var book in await account.GetLibraryAsync()) Books.Add(book);
        }
        catch (Exception ex) { await navigation.AlertAsync("کتابخانه من", ex.Message); }
        finally { IsBusy = false; }
    }

    public Task OpenAsync(BooksDTO book) => navigation.OpenBookAsync(book);

    public async Task RemoveAsync(BooksDTO book)
    {
        if (!await navigation.ConfirmAsync("حذف از کتابخانه", $"«{book.Title}» از کتابخانه شخصی حذف شود؟ فایل دانلود و پیشرفت مطالعه حفظ می‌شود.", "حذف", "انصراف")) return;
        await account.RemoveFromLibraryAsync(book.Id);
        Books.Remove(book);
    }
}
