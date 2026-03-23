using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenderPreference",
                table: "Shops",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GenderPreference",
                table: "SalonOwnerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenderPreference",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GenderPreference",
                table: "SalonOwnerApplications");
        }
    }
}
