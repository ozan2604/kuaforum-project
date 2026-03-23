using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceSalonApplicationForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxNumber",
                table: "SalonOwnerApplications",
                newName: "Street");

            migrationBuilder.AddColumn<string>(
                name: "BuildingNumber",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingNumber",
                table: "SalonOwnerApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "SalonOwnerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "SalonOwnerApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "SalonOwnerApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BuildingNumber",
                table: "SalonOwnerApplications");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "SalonOwnerApplications");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "SalonOwnerApplications");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "SalonOwnerApplications");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "SalonOwnerApplications",
                newName: "TaxNumber");
        }
    }
}
