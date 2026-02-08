using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateEmployeeSkillsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopEmployeeServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEmployeeServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopEmployeeServices_ShopEmployees_ShopEmployeeId",
                        column: x => x.ShopEmployeeId,
                        principalTable: "ShopEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopEmployeeServices_ShopServices_ShopServiceId",
                        column: x => x.ShopServiceId,
                        principalTable: "ShopServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployeeServices_ShopEmployeeId",
                table: "ShopEmployeeServices",
                column: "ShopEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopEmployeeServices_ShopServiceId",
                table: "ShopEmployeeServices",
                column: "ShopServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopEmployeeServices");
        }
    }
}
