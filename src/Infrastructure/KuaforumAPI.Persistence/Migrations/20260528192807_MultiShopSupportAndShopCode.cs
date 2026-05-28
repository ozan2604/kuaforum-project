using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiShopSupportAndShopCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Shops_OwnerId",
                table: "Shops");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Shops",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Shops_Code",
                table: "Shops",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Shops_Code",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Shops");

            migrationBuilder.CreateIndex(
                name: "UQ_Shops_OwnerId",
                table: "Shops",
                column: "OwnerId",
                unique: true);
        }
    }
}
