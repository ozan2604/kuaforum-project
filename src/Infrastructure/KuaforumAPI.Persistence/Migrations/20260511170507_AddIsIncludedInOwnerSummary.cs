using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsIncludedInOwnerSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: true → mevcut satırlar "zaten bildirildi" sayılır, eski randevulara özet SMS gitmez.
            // Yeni randevular entity'den her zaman false olarak eklenir (C# property default'u).
            migrationBuilder.AddColumn<bool>(
                name: "IsIncludedInOwnerSummary",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncludedInOwnerSummary",
                table: "Appointments");
        }
    }
}
