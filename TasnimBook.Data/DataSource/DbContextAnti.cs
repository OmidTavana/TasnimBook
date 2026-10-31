namespace TasnimBook.Data.DataSource
{

    using Microsoft.EntityFrameworkCore;

    public class DbContextAnti : DbContext
    {
        public DbContextAnti(DbContextOptions options) : base(options)
        {

        }
        public DbSet<Author> Authors { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BookCase> BookCases { get; set; }
        public DbSet<BookCat> BookCategories { get; set; }
        public DbSet<JwtUser> JwtUser { get; set; }
        public DbSet<AppVersion> AppVersions { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<ManyToManyBookAndBookCatcs> BookCategoryRelations { get; set; }
        public DbSet<Comments> Comments { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<ReadingProgress> ReadingProgresses { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>()
                .HasMany(e => e.Books)
                .WithOne(e => e.Author)
                .HasForeignKey(e => e.AuthorId);


            ///many to many relation for Book and BookCat
            modelBuilder.Entity<ManyToManyBookAndBookCatcs>()
                .HasKey(bc => new { bc.BookId, bc.BookCategoryId });
            modelBuilder.Entity<ManyToManyBookAndBookCatcs>()
                .HasOne(b => b.Books)
                .WithMany(bc => bc.BookCategories)
                .HasForeignKey(rel => rel.BookId );

            modelBuilder.Entity<ManyToManyBookAndBookCatcs>()
            .HasOne(bc => bc.BookCategories)
            .WithMany(b => b.Books)
            .HasForeignKey(rel => rel.BookCategoryId);
            //end m*n rel



            modelBuilder.Entity<Book>()
                .HasMany(e => e.Comments)
                .WithOne(e => e.Books)
                .HasForeignKey(e => e.BookId);

            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<UserFavorite>()
                .HasKey(favorite => new { favorite.UserId, favorite.BookId });
            modelBuilder.Entity<UserFavorite>()
                .HasOne(favorite => favorite.User)
                .WithMany(user => user.Favorites)
                .HasForeignKey(favorite => favorite.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserFavorite>()
                .HasOne(favorite => favorite.Book)
                .WithMany()
                .HasForeignKey(favorite => favorite.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReadingProgress>()
                .HasKey(progress => new { progress.UserId, progress.BookId });
            modelBuilder.Entity<ReadingProgress>()
                .HasOne(progress => progress.User)
                .WithMany(user => user.ReadingProgresses)
                .HasForeignKey(progress => progress.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ReadingProgress>()
                .HasOne(progress => progress.Book)
                .WithMany()
                .HasForeignKey(progress => progress.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRefreshToken>()
                .HasIndex(token => token.TokenHash)
                .IsUnique();
            modelBuilder.Entity<UserRefreshToken>()
                .HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //end m*n rel




        




        }
    }
}
