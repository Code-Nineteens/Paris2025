using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "src_product_id",
                table: "order",
                newName: "src_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "src_order_id",
                table: "order",
                newName: "src_product_id");
        }
    }
}
