using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product");

            migrationBuilder.AlterColumn<Guid>(
                name: "variant_id",
                table: "order_product",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product",
                column: "variant_id",
                principalTable: "variant",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product");

            migrationBuilder.AlterColumn<Guid>(
                name: "variant_id",
                table: "order_product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product",
                column: "variant_id",
                principalTable: "variant",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
