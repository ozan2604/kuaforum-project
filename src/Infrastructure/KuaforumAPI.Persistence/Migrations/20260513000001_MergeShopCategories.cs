using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuaforumAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeShopCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Piercing (6) → Dövme & Piercing (5)
            migrationBuilder.Sql(
                "UPDATE ShopCategoryAssignments SET CategoryValue = 5 WHERE CategoryValue = 6");

            // Kaş & Kirpik (13) → Makyaj & Kaş/Kirpik (12)
            migrationBuilder.Sql(
                "UPDATE ShopCategoryAssignments SET CategoryValue = 12 WHERE CategoryValue = 13");

            // Aynı dükkanın artık duplicate 5 veya 12'si olabilir, temizle
            migrationBuilder.Sql(@"
                DELETE FROM ShopCategoryAssignments
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM ShopCategoryAssignments
                    GROUP BY ShopId, CategoryValue
                )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma: birleştirilmiş kategorileri ayırmak mümkün değil (veri kaybı olur)
        }
    }
}
