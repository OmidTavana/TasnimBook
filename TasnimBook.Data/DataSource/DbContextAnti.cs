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

            //end m*n rel




        




        }
    }
}
