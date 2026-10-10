using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasnimBook.Data.Migrations
{
    /// <inheritdoc />
    public partial class initDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appversion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    versionname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    versionnumber = table.Column<int>(type: "int", nullable: false),
                    releasenotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    releasedate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    platformid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appversion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Author",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    family = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Author", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Bookcase",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    iduser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookcase", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Bookcat",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    catname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookcat", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jwtuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    regdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    uniquecode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jwtuser", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    imageurl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    des = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    filesize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pagescount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    idauthor = table.Column<int>(type: "int", nullable: true),
                    likescount = table.Column<int>(type: "int", nullable: true),
                    score = table.Column<int>(type: "int", nullable: true),
                    IsSuggested = table.Column<bool>(type: "bit", nullable: true),
                    IsAudioBook = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.id);
                    table.ForeignKey(
                        name: "FK_Books_Author_idauthor",
                        column: x => x.idauthor,
                        principalTable: "Author",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refreshtoken",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isused = table.Column<bool>(type: "bit", nullable: false),
                    jwtid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isrevorked = table.Column<bool>(type: "bit", nullable: false),
                    addeddate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expirydate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refreshtoken", x => x.id);
                    table.ForeignKey(
                        name: "FK_refreshtoken_jwtuser_userid",
                        column: x => x.userid,
                        principalTable: "jwtuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    authoranswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    showinsite = table.Column<bool>(type: "bit", nullable: true),
                    idbook = table.Column<int>(type: "int", nullable: true),
                    iduser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Comments_Books_idbook",
                        column: x => x.idbook,
                        principalTable: "Books",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ManyToManyBookAndBookCatcs",
                columns: table => new
                {
                    bookId = table.Column<int>(type: "int", nullable: false),
                    bookCatId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManyToManyBookAndBookCatcs", x => new { x.bookId, x.bookCatId });
                    table.ForeignKey(
                        name: "FK_ManyToManyBookAndBookCatcs_Bookcat_bookCatId",
                        column: x => x.bookCatId,
                        principalTable: "Bookcat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManyToManyBookAndBookCatcs_Books_bookId",
                        column: x => x.bookId,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_idauthor",
                table: "Books",
                column: "idauthor");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_idbook",
                table: "Comments",
                column: "idbook");

            migrationBuilder.CreateIndex(
                name: "IX_ManyToManyBookAndBookCatcs_bookCatId",
                table: "ManyToManyBookAndBookCatcs",
                column: "bookCatId");

            migrationBuilder.CreateIndex(
                name: "IX_refreshtoken_userid",
                table: "refreshtoken",
                column: "userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appversion");

            migrationBuilder.DropTable(
                name: "Bookcase");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ManyToManyBookAndBookCatcs");

            migrationBuilder.DropTable(
                name: "refreshtoken");

            migrationBuilder.DropTable(
                name: "Bookcat");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "jwtuser");

            migrationBuilder.DropTable(
                name: "Author");
        }
    }
}
