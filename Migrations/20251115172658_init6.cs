using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class init6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "order_product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_order_product_variant_id",
                table: "order_product",
                column: "variant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product",
                column: "variant_id",
                principalTable: "variant",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_product_variant_variant_id",
                table: "order_product");

            migrationBuilder.DropIndex(
                name: "ix_order_product_variant_id",
                table: "order_product");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "order_product");
        }
    }
}
