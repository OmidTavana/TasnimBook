using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TasnimBook.Data.DataSource;

#nullable disable

namespace TasnimBook.Data.Migrations;

[DbContext(typeof(DbContextAnti))]
[Migration("20261106010000_AddEngagementAndAdminRoles")]
public sealed class AddEngagementAndAdminRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "role", table: "appuser", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: UserRoles.User);
        migrationBuilder.AddColumn<string>(name: "adminstatus", table: "appuser", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: AdminApprovalStatuses.NotApplicable);
        migrationBuilder.AddColumn<string>(name: "adminreviewnote", table: "appuser", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "adminreviewedatutc", table: "appuser", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(name: "adminreviewedbyuserid", table: "appuser", type: "int", nullable: true);

        CreateUserBookTable(migrationBuilder, "booklike", new[]
        {
            new ExtraColumn("createdatutc", "datetime2", false)
        });
        CreateUserBookTable(migrationBuilder, "bookdownload", new[]
        {
            new ExtraColumn("downloadedatutc", "datetime2", false)
        });
        CreateUserBookTable(migrationBuilder, "userlibrarybook", new[]
        {
            new ExtraColumn("addedatutc", "datetime2", false),
            new ExtraColumn("lastreadatutc", "datetime2", true),
            new ExtraColumn("lastdownloadedatutc", "datetime2", true)
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
                table.CheckConstraint("CK_bookrating_value", "[value] BETWEEN 1 AND 5");
                table.ForeignKey("FK_bookrating_appuser_userid", x => x.userid, "appuser", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_bookrating_Books_bookid", x => x.bookid, "Books", "id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_bookrating_bookid", "bookrating", "bookid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("bookrating");
        migrationBuilder.DropTable("booklike");
        migrationBuilder.DropTable("bookdownload");
        migrationBuilder.DropTable("userlibrarybook");
        migrationBuilder.DropColumn("role", "appuser");
        migrationBuilder.DropColumn("adminstatus", "appuser");
        migrationBuilder.DropColumn("adminreviewnote", "appuser");
        migrationBuilder.DropColumn("adminreviewedatutc", "appuser");
        migrationBuilder.DropColumn("adminreviewedbyuserid", "appuser");
    }

    private static void CreateUserBookTable(MigrationBuilder migrationBuilder, string name, IReadOnlyList<ExtraColumn> extras)
    {
        var extraSql = string.Join(",", extras.Select(column => $"[{column.Name}] {column.Type} {(column.Nullable ? "NULL" : "NOT NULL")}"));
        migrationBuilder.Sql($$"""
            CREATE TABLE [{{name}}] (
                [userid] int NOT NULL,
                [bookid] int NOT NULL,
                {{extraSql}},
                CONSTRAINT [PK_{{name}}] PRIMARY KEY ([userid], [bookid]),
                CONSTRAINT [FK_{{name}}_appuser_userid] FOREIGN KEY ([userid]) REFERENCES [appuser] ([id]) ON DELETE CASCADE,
                CONSTRAINT [FK_{{name}}_Books_bookid] FOREIGN KEY ([bookid]) REFERENCES [Books] ([id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_{{name}}_bookid] ON [{{name}}] ([bookid]);
            """);
    }

    private sealed record ExtraColumn(string Name, string Type, bool Nullable);
}
