using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiShopSupportAndShopCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Shops_OwnerId' AND object_id = OBJECT_ID('Shops'))
                    DROP INDEX [UQ_Shops_OwnerId] ON [Shops];
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Code' AND object_id = OBJECT_ID('Shops'))
                    ALTER TABLE [Shops] ADD [Code] nvarchar(10) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Shops_Code' AND object_id = OBJECT_ID('Shops'))
                    CREATE UNIQUE INDEX [UQ_Shops_Code] ON [Shops] ([Code]) WHERE [Code] IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Shops_Code",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Shops");

            migrationBuilder.CreateIndex(
                name: "UQ_Shops_OwnerId",
                table: "Shops",
                column: "OwnerId",
                unique: true);
        }
    }
}
