using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewReminderSentToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReviewReminderSent",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReviewReminderSent",
                table: "Appointments");
        }
    }
}
