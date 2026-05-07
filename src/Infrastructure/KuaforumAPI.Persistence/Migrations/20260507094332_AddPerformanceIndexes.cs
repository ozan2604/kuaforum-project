using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shops_OwnerId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_ShopEmployees_ShopId",
                table: "ShopEmployees");

            migrationBuilder.DropIndex(
                name: "IX_ShopEmployees_UserId",
                table: "ShopEmployees");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ShopEmployeeId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_Owner_Active",
                table: "Shops",
                columns: new[] { "OwnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployees_Shop_Status",
                table: "ShopEmployees",
                columns: new[] { "ShopId", "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployees_User_Deleted",
                table: "ShopEmployees",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Employee_Status_Time",
                table: "Appointments",
                columns: new[] { "ShopEmployeeId", "Status", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_GroupId",
                table: "Appointments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_User_Time",
                table: "Appointments",
                columns: new[] { "UserId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shops_Owner_Active",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_ShopEmployees_Shop_Status",
                table: "ShopEmployees");

            migrationBuilder.DropIndex(
                name: "IX_ShopEmployees_User_Deleted",
                table: "ShopEmployees");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Employee_Status_Time",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_GroupId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_User_Time",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_OwnerId",
                table: "Shops",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployees_ShopId",
                table: "ShopEmployees",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployees_UserId",
                table: "ShopEmployees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ShopEmployeeId",
                table: "Appointments",
                column: "ShopEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");
        }
    }
}
