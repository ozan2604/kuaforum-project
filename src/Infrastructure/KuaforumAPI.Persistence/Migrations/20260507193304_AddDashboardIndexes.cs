using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopServices_ShopId",
                table: "ShopServices");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSchedules_ShopEmployeeId",
                table: "EmployeeSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ShopId",
                table: "Appointments");

            migrationBuilder.RenameIndex(
                name: "IX_ShopEmployeeServices_ShopEmployeeId",
                table: "ShopEmployeeServices",
                newName: "IX_ShopEmployeeServices_EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopServices_ShopId_Status",
                table: "ShopServices",
                columns: new[] { "ShopId", "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSchedules_Employee_IsWorking",
                table: "EmployeeSchedules",
                columns: new[] { "ShopEmployeeId", "IsWorking" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ShopId_StartTime",
                table: "Appointments",
                columns: new[] { "ShopId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopServices_ShopId_Status",
                table: "ShopServices");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSchedules_Employee_IsWorking",
                table: "EmployeeSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ShopId_StartTime",
                table: "Appointments");

            migrationBuilder.RenameIndex(
                name: "IX_ShopEmployeeServices_EmployeeId",
                table: "ShopEmployeeServices",
                newName: "IX_ShopEmployeeServices_ShopEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopServices_ShopId",
                table: "ShopServices",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSchedules_ShopEmployeeId",
                table: "EmployeeSchedules",
                column: "ShopEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ShopId",
                table: "Appointments",
                column: "ShopId");
        }
    }
}
