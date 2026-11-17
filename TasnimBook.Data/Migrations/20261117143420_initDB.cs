using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasnimBook.Data.Migrations
{
    /// <inheritdoc />
    public partial class initDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    normalizedemail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    displayname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    passwordhash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lastloginatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false),
                    role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    adminstatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    adminreviewnote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    adminreviewedatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    adminreviewedbyuserid = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appuser", x => x.id);
                });

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
                name: "userrefreshtoken",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userid = table.Column<int>(type: "int", nullable: false),
                    tokenhash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiresatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revokedatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    replacedbytokenhash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    devicename = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userrefreshtoken", x => x.id);
                    table.ForeignKey(
                        name: "FK_userrefreshtoken_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "bookdownload",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    downloadedatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookdownload", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_bookdownload_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookdownload_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booklike",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booklike", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_booklike_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booklike_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookrating",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<byte>(type: "tinyint", nullable: false),
                    createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookrating", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_bookrating_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookrating_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
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

            migrationBuilder.CreateTable(
                name: "readingprogress",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    positionpercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    currentpage = table.Column<int>(type: "int", nullable: true),
                    totalpages = table.Column<int>(type: "int", nullable: true),
                    updatedatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_readingprogress", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_readingprogress_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_readingprogress_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userfavorite",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userfavorite", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_userfavorite_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userfavorite_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userlibrarybook",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    bookid = table.Column<int>(type: "int", nullable: false),
                    addedatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lastreadatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lastdownloadedatutc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userlibrarybook", x => new { x.userid, x.bookid });
                    table.ForeignKey(
                        name: "FK_userlibrarybook_Books_bookid",
                        column: x => x.bookid,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userlibrarybook_appuser_userid",
                        column: x => x.userid,
                        principalTable: "appuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appuser_normalizedemail",
                table: "appuser",
                column: "normalizedemail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookdownload_bookid",
                table: "bookdownload",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_booklike_bookid",
                table: "booklike",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_bookrating_bookid",
                table: "bookrating",
                column: "bookid");

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
                name: "IX_readingprogress_bookid",
                table: "readingprogress",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_refreshtoken_userid",
                table: "refreshtoken",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_userfavorite_bookid",
                table: "userfavorite",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_userlibrarybook_bookid",
                table: "userlibrarybook",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_userrefreshtoken_tokenhash",
                table: "userrefreshtoken",
                column: "tokenhash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_userrefreshtoken_userid",
                table: "userrefreshtoken",
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
                name: "bookdownload");

            migrationBuilder.DropTable(
                name: "booklike");

            migrationBuilder.DropTable(
                name: "bookrating");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ManyToManyBookAndBookCatcs");

            migrationBuilder.DropTable(
                name: "readingprogress");

            migrationBuilder.DropTable(
                name: "refreshtoken");

            migrationBuilder.DropTable(
                name: "userfavorite");

            migrationBuilder.DropTable(
                name: "userlibrarybook");

            migrationBuilder.DropTable(
                name: "userrefreshtoken");

            migrationBuilder.DropTable(
                name: "Bookcat");

            migrationBuilder.DropTable(
                name: "jwtuser");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "appuser");

            migrationBuilder.DropTable(
                name: "Author");
        }
    }
}
