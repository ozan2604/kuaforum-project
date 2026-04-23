using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleShopCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "SalonOwnerApplications");

            migrationBuilder.CreateTable(
                name: "SalonApplicationCategoryItems",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonApplicationCategoryItems", x => new { x.ApplicationId, x.CategoryValue });
                    table.ForeignKey(
                        name: "FK_SalonApplicationCategoryItems_SalonOwnerApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "SalonOwnerApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopCategoryAssignments",
                columns: table => new
                {
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCategoryAssignments", x => new { x.ShopId, x.CategoryValue });
                    table.ForeignKey(
                        name: "FK_ShopCategoryAssignments_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalonApplicationCategoryItems");

            migrationBuilder.DropTable(
                name: "ShopCategoryAssignments");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "SalonOwnerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
