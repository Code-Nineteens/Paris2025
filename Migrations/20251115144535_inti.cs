using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Paris2025.Migrations
{
    /// <inheritdoc />
    public partial class inti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "__EndpointMap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    path = table.Column<string>(type: "text", nullable: false),
                    http_method = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    accesses = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk___endpoint_map", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    src_product_id = table.Column<long>(type: "bigint", nullable: false),
                    order_name = table.Column<string>(type: "text", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancel_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<int>(type: "integer", nullable: false),
                    presentment_currency = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    fulfillment_status = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_discounts = table.Column<decimal>(type: "numeric", nullable: false),
                    total_shipping = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tax = table.Column<decimal>(type: "numeric", nullable: false),
                    total_refunded = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tip = table.Column<decimal>(type: "numeric", nullable: false),
                    confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    test = table.Column<bool>(type: "boolean", nullable: false),
                    closed = table.Column<bool>(type: "boolean", nullable: false),
                    taxexempt = table.Column<bool>(type: "boolean", nullable: false),
                    taxes_included = table.Column<bool>(type: "boolean", nullable: false),
                    duties_included = table.Column<bool>(type: "boolean", nullable: false),
                    fulfillable = table.Column<bool>(type: "boolean", nullable: true),
                    requires_shipping = table.Column<bool>(type: "boolean", nullable: true),
                    customer_accepts_marketing = table.Column<bool>(type: "boolean", nullable: true),
                    billing_address_matches_shipping_address = table.Column<bool>(type: "boolean", nullable: true),
                    can_mark_as_paid = table.Column<bool>(type: "boolean", nullable: true),
                    cannot_notify_customer = table.Column<bool>(type: "boolean", nullable: true),
                    note = table.Column<string>(type: "text", nullable: false),
                    source_name = table.Column<string>(type: "text", nullable: false),
                    source_identifier = table.Column<string>(type: "text", nullable: false),
                    confirmation_number = table.Column<string>(type: "text", nullable: false),
                    po_number = table.Column<string>(type: "text", nullable: false),
                    client_ip = table.Column<string>(type: "text", nullable: false),
                    customer_locale = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<string>(type: "text", nullable: false),
                    customer_email = table.Column<string>(type: "text", nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    billing_address_1 = table.Column<string>(type: "text", nullable: false),
                    billing_address_2 = table.Column<string>(type: "text", nullable: false),
                    billing_city = table.Column<string>(type: "text", nullable: false),
                    billing_province = table.Column<string>(type: "text", nullable: false),
                    billing_country = table.Column<string>(type: "text", nullable: false),
                    billing_zip = table.Column<string>(type: "text", nullable: false),
                    shipping_address_1 = table.Column<string>(type: "text", nullable: false),
                    shipping_address_2 = table.Column<string>(type: "text", nullable: false),
                    shipping_city = table.Column<string>(type: "text", nullable: false),
                    shipping_province = table.Column<string>(type: "text", nullable: false),
                    shipping_country = table.Column<string>(type: "text", nullable: false),
                    shipping_zip = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    src_product_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    vendor = table.Column<string>(type: "text", nullable: false),
                    product_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_inventory = table.Column<int>(type: "integer", nullable: false),
                    price_min = table.Column<decimal>(type: "numeric", nullable: false),
                    price_max = table.Column<decimal>(type: "numeric", nullable: false),
                    price_current = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    description_html = table.Column<string>(type: "text", nullable: false),
                    seo_title = table.Column<string>(type: "text", nullable: false),
                    seo_description = table.Column<string>(type: "text", nullable: false),
                    has_only_default_variant = table.Column<bool>(type: "boolean", nullable: false),
                    has_out_of_stock_variants = table.Column<bool>(type: "boolean", nullable: false),
                    is_gift_card = table.Column<bool>(type: "boolean", nullable: false),
                    requires_selling_plan = table.Column<bool>(type: "boolean", nullable: false),
                    category_id = table.Column<string>(type: "text", nullable: false),
                    category_name = table.Column<string>(type: "text", nullable: false),
                    category_full_name = table.Column<string>(type: "text", nullable: false),
                    inventory_title = table.Column<string>(type: "text", nullable: true),
                    inventory_vendor = table.Column<string>(type: "text", nullable: true),
                    inventory_product_type = table.Column<string>(type: "text", nullable: true),
                    inventory_status = table.Column<int>(type: "integer", nullable: true),
                    inventory_total_inventory = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_product",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_product_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_product_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_product",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variant_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_variant_product_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    src_product_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    vendor = table.Column<string>(type: "text", nullable: false),
                    product_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_inventory = table.Column<int>(type: "integer", nullable: false),
                    price_min = table.Column<decimal>(type: "numeric", nullable: false),
                    price_max = table.Column<decimal>(type: "numeric", nullable: false),
                    price_current = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    description_html = table.Column<string>(type: "text", nullable: false),
                    seo_title = table.Column<string>(type: "text", nullable: false),
                    seo_description = table.Column<string>(type: "text", nullable: false),
                    has_only_default_variant = table.Column<bool>(type: "boolean", nullable: false),
                    has_out_of_stock_variants = table.Column<bool>(type: "boolean", nullable: false),
                    is_gift_card = table.Column<bool>(type: "boolean", nullable: false),
                    requires_selling_plan = table.Column<bool>(type: "boolean", nullable: false),
                    category_id = table.Column<string>(type: "text", nullable: false),
                    category_name = table.Column<string>(type: "text", nullable: false),
                    category_full_name = table.Column<string>(type: "text", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variant", x => x.id);
                    table.ForeignKey(
                        name: "fk_variant_variant_product_product_id",
                        column: x => x.product_id,
                        principalTable: "variant_product",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_product_order_id",
                table: "order_product",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_product_product_id",
                table: "order_product",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_variant_product_id",
                table: "variant",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variant_product_product_id",
                table: "variant_product",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__EndpointMap");

            migrationBuilder.DropTable(
                name: "order_product");

            migrationBuilder.DropTable(
                name: "variant");

            migrationBuilder.DropTable(
                name: "order");

            migrationBuilder.DropTable(
                name: "variant_product");

            migrationBuilder.DropTable(
                name: "product");
        }
    }
}
