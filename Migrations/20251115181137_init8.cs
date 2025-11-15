using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inventory_product_type",
                table: "variant");

            migrationBuilder.DropColumn(
                name: "inventory_status",
                table: "variant");

            migrationBuilder.DropColumn(
                name: "inventory_title",
                table: "variant");

            migrationBuilder.DropColumn(
                name: "inventory_total_inventory",
                table: "variant");

            migrationBuilder.DropColumn(
                name: "inventory_vendor",
                table: "variant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inventory_product_type",
                table: "variant",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inventory_status",
                table: "variant",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inventory_title",
                table: "variant",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inventory_total_inventory",
                table: "variant",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inventory_vendor",
                table: "variant",
                type: "text",
                nullable: true);
        }
    }
}
