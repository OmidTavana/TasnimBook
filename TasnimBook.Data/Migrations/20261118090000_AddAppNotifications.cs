using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TasnimBook.Data.DataSource;

#nullable disable

namespace TasnimBook.Data.Migrations;

[DbContext(typeof(DbContextAnti))]
[Migration("20261118090000_AddAppNotifications")]
public sealed class AddAppNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "appnotification",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                notificationtype = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                actionurl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                versionname = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                versionnumber = table.Column<int>(type: "int", nullable: true),
                platformid = table.Column<int>(type: "int", nullable: true),
                publishatutc = table.Column<DateTime>(type: "datetime2", nullable: false),
                expiresatutc = table.Column<DateTime>(type: "datetime2", nullable: true),
                isactive = table.Column<bool>(type: "bit", nullable: false),
                createdatutc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_appnotification", x => x.id));

        migrationBuilder.CreateIndex(
            name: "IX_appnotification_active_publish",
            table: "appnotification",
            columns: new[] { "isactive", "publishatutc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "appnotification");
    }
}
