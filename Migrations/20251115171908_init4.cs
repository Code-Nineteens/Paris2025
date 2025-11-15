using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_shipping",
                table: "order_product",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "taxable",
                table: "order_product",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requires_shipping",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "taxable",
                table: "order_product");
        }
    }
}
