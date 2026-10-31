using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TasnimBook.Data.DataSource;

#nullable disable

namespace TasnimBook.Data.Migrations;

[DbContext(typeof(DbContextAnti))]
[Migration("20261031143000_AddUserAccounts")]
public sealed class AddUserAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "appuser",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                normalizedemail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                displayname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                passwordhash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                lastloginatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                isactive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_appuser", x => x.id));

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
                table.ForeignKey("FK_readingprogress_Books_bookid", x => x.bookid, "Books", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_readingprogress_appuser_userid", x => x.userid, "appuser", "id", onDelete: ReferentialAction.Cascade);
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
                table.ForeignKey("FK_userfavorite_Books_bookid", x => x.bookid, "Books", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_userfavorite_appuser_userid", x => x.userid, "appuser", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "userrefreshtoken",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
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
                table.ForeignKey("FK_userrefreshtoken_appuser_userid", x => x.userid, "appuser", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_appuser_normalizedemail", "appuser", "normalizedemail", unique: true);
        migrationBuilder.CreateIndex("IX_readingprogress_bookid", "readingprogress", "bookid");
        migrationBuilder.CreateIndex("IX_userfavorite_bookid", "userfavorite", "bookid");
        migrationBuilder.CreateIndex("IX_userrefreshtoken_tokenhash", "userrefreshtoken", "tokenhash", unique: true);
        migrationBuilder.CreateIndex("IX_userrefreshtoken_userid", "userrefreshtoken", "userid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("readingprogress");
        migrationBuilder.DropTable("userfavorite");
        migrationBuilder.DropTable("userrefreshtoken");
        migrationBuilder.DropTable("appuser");
    }
}
