using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Revoked_CreatedAt",
                table: "RefreshTokens",
                columns: new[] { "IsRevoked", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_CreatedAt",
                table: "OtpCodes",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Revoked_CreatedAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_CreatedAt",
                table: "OtpCodes");
        }
    }
}
