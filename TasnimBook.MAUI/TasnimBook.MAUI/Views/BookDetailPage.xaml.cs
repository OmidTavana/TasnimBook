using TasnimBook.Data;
using TasnimBook.Data.Models;
using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.Views;

public partial class BookDetailPage : ContentPage
{
    private readonly Book _book;
    private readonly BooksDTO _dto;
    private readonly AccountService _account;
    private readonly BookAssetService _assets;
    private bool _isFavorite;
    private bool _isDownloading;

    public BookDetailPage(Book book, BooksDTO dto, AccountService account, BookAssetService assets)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _book = book;
        _dto = dto;
        _account = account;
        _assets = assets;
        CoverImage.Source = "book_cover_placeholder.svg";
        TitleLabel.Text = dto.Title;
        AuthorLabel.Text = BookUi.Author(book);
        PagesLabel.Text = dto.PagesCount ?? "—";
        SizeLabel.Text = dto.FileSize ?? "—";
        ScoreLabel.Text = book.Score is > 0 ? $"{book.Score}/5" : "—";
        DescriptionLabel.Text = string.IsNullOrWhiteSpace(book.Description) ? dto.Description ?? "توضیحی برای این کتاب ثبت نشده است." : book.Description;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var imageUrl = string.IsNullOrWhiteSpace(_dto.ImageUrl) ? _book.ImageUrl : _dto.ImageUrl;
        var coverPath = await _assets.GetCoverPathAsync(_book.Id, imageUrl);
        CoverImage.Source = string.IsNullOrWhiteSpace(coverPath) ? "book_cover_placeholder.svg" : ImageSource.FromFile(coverPath);
        try
        {
            var progress = await _account.GetProgressAsync(_book.Id);
            ProgressLabel.Text = progress > 0 ? $"{progress:0}% مطالعه شده" : "هنوز شروع نشده";
            ReadButton.Text = progress > 0 ? "ادامه مطالعه" : "شروع مطالعه در برنامه";
            _isFavorite = await _account.IsFavoriteAsync(_book.Id);
            UpdateFavoriteUi();
        }
        catch { }
    }

    private async void ReadClicked(object sender, EventArgs e)
    {
        var localPath = await EnsureDownloadedAsync();
        if (localPath is not null)
            await Navigation.PushAsync(new BookReaderPage(_book, _dto, _account, localPath));
    }

    private async void FavoriteClicked(object? sender, EventArgs e)
    {
        await _account.InitializeAsync();
        if (!_account.IsLoggedIn)
        {
            var login = await DisplayAlert("حساب کاربری", "برای همگام‌سازی علاقه‌مندی‌ها وارد حساب شوید.", "ورود / ثبت‌نام", "فعلاً نه");
            if (login) await Navigation.PushAsync(new AccountPage(_account, null, _assets));
            return;
        }
        try
        {
            _isFavorite = await _account.SetFavoriteAsync(_book.Id, !_isFavorite);
            UpdateFavoriteUi();
        }
        catch (Exception ex) { await DisplayAlert("علاقه‌مندی", ex.Message, "باشه"); }
    }

    private void UpdateFavoriteUi()
    {
        FavoriteButton.Text = _isFavorite ? "♥" : "♡";
        FavoriteActionButton.Text = _isFavorite ? "♥  ذخیره شده" : "♡  علاقه‌مندی";
    }

    private async void ShareClicked(object sender, EventArgs e) => await Share.Default.RequestAsync(new ShareTextRequest
    {
        Title = _dto.Title, Text = $"{_dto.Title}\n{BookUi.Author(_book)}", Uri = GetBookUrl()
    });

    private async void DownloadClicked(object sender, EventArgs e)
    {
        var localPath = await EnsureDownloadedAsync();
        if (localPath is not null)
            await DisplayAlert("دریافت کامل شد", $"PDF برای مطالعه آفلاین در پوشه Downloads برنامه ذخیره شد.\n{localPath}", "باشه");
    }

    private async Task<string?> EnsureDownloadedAsync()
    {
        if (_isDownloading) return null;
        _isDownloading = true;
        DownloadPanel.IsVisible = true;
        ReadButton.IsEnabled = false;
        DownloadButton.IsEnabled = false;
        DownloadStatusLabel.Text = "در حال دریافت PDF…";
        var progress = new Progress<double>(value =>
        {
            value = Math.Clamp(value, 0, 1);
            DownloadProgressBar.Progress = value;
            DownloadPercentLabel.Text = $"{value * 100:0}%";
        });
        try
        {
            var path = await _assets.EnsurePdfAsync(_book, _dto, progress);
            DownloadStatusLabel.Text = "PDF آماده مطالعه آفلاین است";
            DownloadPercentLabel.Text = "۱۰۰٪";
            DownloadButton.Text = "✓  دریافت شده";
            return path;
        }
        catch (Exception ex)
        {
            DownloadPanel.IsVisible = false;
            await DisplayAlert("خطا در دریافت", ex.Message, "باشه");
            return null;
        }
        finally
        {
            _isDownloading = false;
            ReadButton.IsEnabled = true;
            DownloadButton.IsEnabled = true;
        }
    }

    private string GetBookUrl() => string.IsNullOrWhiteSpace(_book.Url) ? _dto.Url ?? string.Empty : _book.Url;
    private async void BackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
