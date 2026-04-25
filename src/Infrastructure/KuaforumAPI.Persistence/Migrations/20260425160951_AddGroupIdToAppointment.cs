using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupIdToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Appointments");
        }
    }
}
