using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TasnimBook.Data.DataSource;

#nullable disable

namespace TasnimBook.Data.Migrations;

[DbContext(typeof(DbContextAnti))]
[Migration("20261124120000_CentralizedAppReleases")]
public sealed class CentralizedAppReleases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "contenttype", table: "appversion", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<long>(name: "filesize", table: "appversion", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "isactive", table: "appversion", type: "bit", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "ismandatory", table: "appversion", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "ispublished", table: "appversion", type: "bit", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<string>(name: "sha256", table: "appversion", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "storedfilename", table: "appversion", type: "nvarchar(260)", maxLength: 260, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_appversion_platform_release", table: "appversion", columns: new[] { "platformid", "ispublished", "isactive" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_appversion_platform_release", table: "appversion");
        migrationBuilder.DropColumn(name: "contenttype", table: "appversion");
        migrationBuilder.DropColumn(name: "filesize", table: "appversion");
        migrationBuilder.DropColumn(name: "isactive", table: "appversion");
        migrationBuilder.DropColumn(name: "ismandatory", table: "appversion");
        migrationBuilder.DropColumn(name: "ispublished", table: "appversion");
        migrationBuilder.DropColumn(name: "sha256", table: "appversion");
        migrationBuilder.DropColumn(name: "storedfilename", table: "appversion");
    }
}
