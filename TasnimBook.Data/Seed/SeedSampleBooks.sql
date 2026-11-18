/*
    WARNING - DESTRUCTIVE SEED SCRIPT
    This script deletes every existing book, category, author and all
    book-related user data before inserting the sample catalog.

    Target database: AntiApi
*/

USE [AntiApi];

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    -- 1) Remove all current book-related data in foreign-key-safe order.
    -------------------------------------------------------------------------
    DELETE FROM dbo.Comments;
    DELETE FROM dbo.ManyToManyBookAndBookCatcs;
    DELETE FROM dbo.bookdownload;
    DELETE FROM dbo.booklike;
    DELETE FROM dbo.bookrating;
    DELETE FROM dbo.readingprogress;
    DELETE FROM dbo.userfavorite;
    DELETE FROM dbo.userlibrarybook;
    DELETE FROM dbo.Books;
    DELETE FROM dbo.Bookcat;
    DELETE FROM dbo.Author;

    DBCC CHECKIDENT ('dbo.Comments', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Books', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Bookcat', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Author', RESEED, 0) WITH NO_INFOMSGS;

    -------------------------------------------------------------------------
    -- 2) Create categories.
    -------------------------------------------------------------------------
    INSERT INTO dbo.Bookcat (catname)
    VALUES
        (N'ادبیات کلاسیک'),
        (N'رمان'),
        (N'فلسفه'),
        (N'علم و فناوری'),
        (N'اقتصاد'),
        (N'کودک و نوجوان'),
        (N'معمایی'),
        (N'وحشت'),
        (N'مدیریت و استراتژی'),
        (N'شعر و عرفان');

    -------------------------------------------------------------------------
    -- 3) Create authors.
    -------------------------------------------------------------------------
    INSERT INTO dbo.Author (name, family)
    VALUES
        (N'جین', N'آستن'),
        (N'فئودور', N'داستایفسکی'),
        (N'افلاطون', N''),
        (N'چارلز', N'داروین'),
        (N'سون', N'تزو'),
        (N'آرتور کانن', N'دویل'),
        (N'مری', N'شلی'),
        (N'لوئیس', N'کارول'),
        (N'آدام', N'اسمیت'),
        (N'مارکوس', N'اورلیوس'),
        (N'جبران', N'خلیل جبران'),
        (N'برام', N'استوکر');

    -------------------------------------------------------------------------
    -- 4) Define the sample catalog.
    -- Covers: tested Open Library image URLs (HTTP 200 / image/jpeg).
    -- PDFs: public test/reference PDFs from W3C and Mozilla PDF.js.
    -- Audio: public-domain LibriVox recordings hosted by Internet Archive.
    -------------------------------------------------------------------------
    DECLARE @SeedBooks TABLE
    (
        Title NVARCHAR(300) NOT NULL PRIMARY KEY,
        AuthorName NVARCHAR(50) NOT NULL,
        AuthorFamily NVARCHAR(50) NOT NULL,
        MediaUrl NVARCHAR(MAX) NOT NULL,
        CoverUrl NVARCHAR(MAX) NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        FileSize NVARCHAR(50) NOT NULL,
        PagesCount NVARCHAR(50) NOT NULL,
        IsSuggested BIT NOT NULL,
        IsAudioBook BIT NOT NULL
    );

    INSERT INTO @SeedBooks
        (Title, AuthorName, AuthorFamily, MediaUrl, CoverUrl, [Description],
         FileSize, PagesCount, IsSuggested, IsAudioBook)
    VALUES
        (N'غرور و تعصب', N'جین', N'آستن',
         N'https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf',
         N'https://covers.openlibrary.org/b/olid/OL24103453M-L.jpg?default=false',
         N'رمانی کلاسیک درباره خانواده، قضاوت‌های شتاب‌زده و شکل‌گیری عشق میان الیزابت بنت و آقای دارسی.',
         N'آنلاین', N'432', 1, 0),

        (N'جنایت و مکافات', N'فئودور', N'داستایفسکی',
         N'https://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf',
         N'https://covers.openlibrary.org/b/olid/OL24103453M-L.jpg?default=false',
         N'روایتی روان‌شناختی درباره گناه، عدالت، فقر و امکان رستگاری.',
         N'آنلاین', N'671', 1, 0),

        (N'جمهور', N'افلاطون', N'',
         N'https://www.w3.org/WAI/WCAG20/versions/guidelines/wcag20-guidelines-20081211-a4.pdf',
         N'https://covers.openlibrary.org/b/olid/OL7242701M-L.jpg?default=false',
         N'گفت‌وگویی بنیادین درباره عدالت، حکومت مطلوب، تربیت و حقیقت.',
         N'آنلاین', N'416', 1, 0),

        (N'خاستگاه گونه‌ها', N'چارلز', N'داروین',
         N'https://www.w3.org/TR/WAI-WEBCONTENT/wai-pageauth.pdf',
         N'https://covers.openlibrary.org/b/olid/OL6462185M-L.jpg?default=false',
         N'اثری مهم درباره تکامل زیستی و انتخاب طبیعی.',
         N'آنلاین', N'513', 0, 0),

        (N'هنر جنگ', N'سون', N'تزو',
         N'https://www.w3.org/WAI/GL/WD-WAI-PAGEAUTH-19990225/wai-pageauth.pdf',
         N'https://covers.openlibrary.org/b/olid/OL7242701M-L.jpg?default=false',
         N'رساله‌ای فشرده درباره راهبرد، شناخت موقعیت و تصمیم‌گیری.',
         N'آنلاین', N'109', 1, 0),

        (N'ماجراهای شرلوک هولمز', N'آرتور کانن', N'دویل',
         N'https://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf',
         N'https://covers.openlibrary.org/b/olid/OL24103453M-L.jpg?default=false',
         N'مجموعه‌ای از پرونده‌های مشهور شرلوک هولمز و استدلال‌های دقیق او.',
         N'آنلاین', N'307', 1, 0),

        (N'فرانکنشتاین', N'مری', N'شلی',
         N'https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf',
         N'https://covers.openlibrary.org/b/olid/OL26435360M-L.jpg?default=false',
         N'داستان دانشمندی که موجودی زنده می‌آفریند و با پیامدهای کار خود روبه‌رو می‌شود.',
         N'آنلاین', N'261', 0, 0),

        (N'ثروت ملل', N'آدام', N'اسمیت',
         N'https://www.w3.org/WAI/WCAG20/versions/guidelines/wcag20-guidelines-20081211-a4.pdf',
         N'https://covers.openlibrary.org/b/olid/OL6462185M-L.jpg?default=false',
         N'یکی از آثار پایه علم اقتصاد درباره بازار، تولید، تجارت و ثروت جوامع.',
         N'آنلاین', N'656', 0, 0),

        (N'تأملات', N'مارکوس', N'اورلیوس',
         N'https://www.w3.org/TR/WAI-WEBCONTENT/wai-pageauth.pdf',
         N'https://covers.openlibrary.org/b/olid/OL7242701M-L.jpg?default=false',
         N'یادداشت‌هایی درباره خویشتن‌داری، وظیفه، آرامش ذهن و فلسفه رواقی.',
         N'آنلاین', N'304', 1, 0),

        (N'دراکولا', N'برام', N'استوکر',
         N'https://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf',
         N'https://covers.openlibrary.org/b/olid/OL26435360M-L.jpg?default=false',
         N'رمانی گوتیک و دلهره‌آور درباره کنت دراکولا و نیروی مرموز او.',
         N'آنلاین', N'381', 0, 0),

        (N'آلیس در سرزمین عجایب - صوتی بخش اول', N'لوئیس', N'کارول',
         N'https://archive.org/download/alice_in_wonderland_librivox/wonderland_ch_01.mp3',
         N'https://covers.openlibrary.org/b/olid/OL24103453M-L.jpg?default=false',
         N'نسخه صوتی رایگان از فصل نخست آلیس در سرزمین عجایب؛ ضبط عمومی LibriVox.',
         N'حدود 5 MB', N'0', 1, 1),

        (N'آلیس در سرزمین عجایب - صوتی بخش دوم', N'لوئیس', N'کارول',
         N'https://archive.org/download/alice_in_wonderland_librivox/wonderland_ch_02.mp3',
         N'https://covers.openlibrary.org/b/olid/OL24103453M-L.jpg?default=false',
         N'نسخه صوتی رایگان از فصل دوم آلیس در سرزمین عجایب؛ ضبط عمومی LibriVox.',
         N'حدود 6 MB', N'0', 1, 1),

        (N'پیامبر - صوتی بخش اول', N'جبران', N'خلیل جبران',
         N'https://archive.org/download/prophet1_1901_librivox/prophet1_01_gibran_128kb.mp3',
         N'https://covers.openlibrary.org/b/olid/OL7242701M-L.jpg?default=false',
         N'بخش اول نسخه صوتی رایگان کتاب پیامبر؛ ضبط عمومی LibriVox.',
         N'حدود 10 MB', N'0', 1, 1),

        (N'پیامبر - صوتی بخش دوم', N'جبران', N'خلیل جبران',
         N'https://archive.org/download/prophet1_1901_librivox/prophet1_02_gibran_128kb.mp3',
         N'https://covers.openlibrary.org/b/olid/OL7242701M-L.jpg?default=false',
         N'بخش دوم نسخه صوتی رایگان کتاب پیامبر؛ ضبط عمومی LibriVox.',
         N'حدود 10 MB', N'0', 1, 1);

    -------------------------------------------------------------------------
    -- 5) Insert books and connect them to their authors.
    -------------------------------------------------------------------------
    INSERT INTO dbo.Books
        (title, url, imageurl, des, [date], filesize, pagescount,
         idauthor, likescount, score, IsSuggested, IsAudioBook)
    SELECT
        seed.Title,
        seed.MediaUrl,
        seed.CoverUrl,
        seed.[Description],
        SYSUTCDATETIME(),
        seed.FileSize,
        seed.PagesCount,
        author.id,
        0,
        0,
        seed.IsSuggested,
        seed.IsAudioBook
    FROM @SeedBooks AS seed
    INNER JOIN dbo.Author AS author
        ON author.name = seed.AuthorName
       AND author.family = seed.AuthorFamily;

    -------------------------------------------------------------------------
    -- 6) Connect books to one or more categories.
    -------------------------------------------------------------------------
    DECLARE @BookCategories TABLE
    (
        BookTitle NVARCHAR(300) NOT NULL,
        CategoryName NVARCHAR(50) NOT NULL,
        PRIMARY KEY (BookTitle, CategoryName)
    );

    INSERT INTO @BookCategories (BookTitle, CategoryName)
    VALUES
        (N'غرور و تعصب', N'ادبیات کلاسیک'),
        (N'غرور و تعصب', N'رمان'),
        (N'جنایت و مکافات', N'ادبیات کلاسیک'),
        (N'جنایت و مکافات', N'رمان'),
        (N'جنایت و مکافات', N'فلسفه'),
        (N'جمهور', N'فلسفه'),
        (N'خاستگاه گونه‌ها', N'علم و فناوری'),
        (N'هنر جنگ', N'مدیریت و استراتژی'),
        (N'هنر جنگ', N'فلسفه'),
        (N'ماجراهای شرلوک هولمز', N'معمایی'),
        (N'ماجراهای شرلوک هولمز', N'ادبیات کلاسیک'),
        (N'فرانکنشتاین', N'وحشت'),
        (N'فرانکنشتاین', N'رمان'),
        (N'ثروت ملل', N'اقتصاد'),
        (N'تأملات', N'فلسفه'),
        (N'دراکولا', N'وحشت'),
        (N'دراکولا', N'ادبیات کلاسیک'),
        (N'دراکولا', N'رمان'),
        (N'آلیس در سرزمین عجایب - صوتی بخش اول', N'کودک و نوجوان'),
        (N'آلیس در سرزمین عجایب - صوتی بخش اول', N'ادبیات کلاسیک'),
        (N'آلیس در سرزمین عجایب - صوتی بخش دوم', N'کودک و نوجوان'),
        (N'آلیس در سرزمین عجایب - صوتی بخش دوم', N'ادبیات کلاسیک'),
        (N'پیامبر - صوتی بخش اول', N'شعر و عرفان'),
        (N'پیامبر - صوتی بخش اول', N'فلسفه'),
        (N'پیامبر - صوتی بخش دوم', N'شعر و عرفان'),
        (N'پیامبر - صوتی بخش دوم', N'فلسفه');

    INSERT INTO dbo.ManyToManyBookAndBookCatcs (bookId, bookCatId)
    SELECT book.id, category.id
    FROM @BookCategories AS mapping
    INNER JOIN dbo.Books AS book ON book.title = mapping.BookTitle
    INNER JOIN dbo.Bookcat AS category ON category.catname = mapping.CategoryName;

    COMMIT TRANSACTION;

    -------------------------------------------------------------------------
    -- 7) Return a compact execution report.
    -------------------------------------------------------------------------
    SELECT
        (SELECT COUNT(*) FROM dbo.Books) AS BooksInserted,
        (SELECT COUNT(*) FROM dbo.Books WHERE IsAudioBook = 1) AS AudioBooksInserted,
        (SELECT COUNT(*) FROM dbo.Bookcat) AS CategoriesInserted,
        (SELECT COUNT(*) FROM dbo.Author) AS AuthorsInserted;

    SELECT
        book.id,
        book.title,
        CASE WHEN book.IsAudioBook = 1 THEN N'Audio/MP3' ELSE N'PDF' END AS MediaType,
        book.url,
        book.imageurl
    FROM dbo.Books AS book
    ORDER BY book.IsAudioBook, book.id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
