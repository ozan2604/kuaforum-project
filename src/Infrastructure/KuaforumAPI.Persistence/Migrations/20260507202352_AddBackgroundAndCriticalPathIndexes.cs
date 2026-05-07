using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundAndCriticalPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopClosureDates_ShopId",
                table: "ShopClosureDates");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveDates_ShopEmployeeId",
                table: "EmployeeLeaveDates");

            migrationBuilder.AlterColumn<string>(
                name: "CircleUserId",
                table: "UserFavoriteShops",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteShops_User_Shop",
                table: "UserFavoriteShops",
                columns: new[] { "CircleUserId", "ShopId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopClosureDates_Shop_Date",
                table: "ShopClosureDates",
                columns: new[] { "ShopId", "ClosureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveDates_Employee_Date",
                table: "EmployeeLeaveDates",
                columns: new[] { "ShopEmployeeId", "LeaveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Reminder2h",
                table: "Appointments",
                columns: new[] { "Status", "Is2hReminderSent", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Reminder48h",
                table: "Appointments",
                columns: new[] { "Status", "Is48hReminderSent", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_EndTime",
                table: "Appointments",
                columns: new[] { "Status", "EndTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserFavoriteShops_User_Shop",
                table: "UserFavoriteShops");

            migrationBuilder.DropIndex(
                name: "IX_ShopClosureDates_Shop_Date",
                table: "ShopClosureDates");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLeaveDates_Employee_Date",
                table: "EmployeeLeaveDates");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Reminder2h",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Reminder48h",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status_EndTime",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "CircleUserId",
                table: "UserFavoriteShops",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ShopClosureDates_ShopId",
                table: "ShopClosureDates",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveDates_ShopEmployeeId",
                table: "EmployeeLeaveDates",
                column: "ShopEmployeeId");
        }
    }
}
