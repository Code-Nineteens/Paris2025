using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "currency",
                table: "order_product",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "discounted_price",
                table: "order_product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "discounted_unit_price",
                table: "order_product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "orginal_unit_price",
                table: "order_product",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "order_product",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "discounted_price",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "discounted_unit_price",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "orginal_unit_price",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "order_product");
        }
    }
}
