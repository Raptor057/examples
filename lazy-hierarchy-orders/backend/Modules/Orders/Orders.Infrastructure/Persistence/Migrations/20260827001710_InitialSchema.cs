using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orders.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Esquema inicial en snake_case, sin identificadores entrecomillados.
    ///
    /// Los indices no son adorno: un arbol perezoso SIN los indices que sus consultas necesitan
    /// es igual de lento que traerlo todo, y encima parece que el patron no sirve. Cada
    /// CreateIndex de abajo lleva escrito a que consulta sirve.
    /// </summary>
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_order",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    placed_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_order", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_line",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_line_customer_order_order_id",
                        column: x => x.order_id,
                        principalTable: "customer_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_line_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Nivel 3 (categorias): recorrido por tenant ya ordenado por nombre, sin sort adicional.
            migrationBuilder.CreateIndex(
                name: "ix_category_tenant_id_name",
                table: "category",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_category_public_id",
                table: "category",
                column: "public_id",
                unique: true);

            // Unicidad POR TENANT, no global: dos empresas pueden usar el mismo codigo de categoria.
            migrationBuilder.CreateIndex(
                name: "ux_category_tenant_id_code",
                table: "category",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            // El indice del que vive el arbol. Sirve a tres cosas:
            //   - nivel 1 (anios): agregacion por rango de fecha dentro del tenant,
            //   - nivel 2 (meses): idem, acotado a un anio,
            //   - CTE del nivel 5: ORDER BY placed_at_utc DESC, id DESC + OFFSET/FETCH.
            // Termina en id porque ese ORDER BY tiene que ser UNICO, o el paginado repite
            // y omite filas. Parcial sobre activos: toda lectura del arbol filtra is_active.
            migrationBuilder.CreateIndex(
                name: "ix_customer_order_tenant_id_placed_at_utc_id",
                table: "customer_order",
                columns: new[] { "tenant_id", "placed_at_utc", "id" },
                filter: "is_active");

            // Busqueda por estado (searchType=status) sin perder el orden por fecha.
            migrationBuilder.CreateIndex(
                name: "ix_customer_order_tenant_id_status_placed_at_utc",
                table: "customer_order",
                columns: new[] { "tenant_id", "status", "placed_at_utc" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_customer_order_public_id",
                table: "customer_order",
                column: "public_id",
                unique: true);

            // Unicidad del numero de pedido POR TENANT.
            migrationBuilder.CreateIndex(
                name: "ux_customer_order_tenant_id_order_number",
                table: "customer_order",
                columns: new[] { "tenant_id", "order_number" },
                unique: true);

            // Indices que EF crea solo, uno por llave foranea. En una tabla de millones de filas
            // cuestan escritura y aqui son casi redundantes con los compuestos de arriba;
            // se conservan porque aceleran la verificacion de la FK al insertar.
            migrationBuilder.CreateIndex(
                name: "ix_order_line_order_id",
                table: "order_line",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_product_id",
                table: "order_line",
                column: "product_id");

            // Hoja (lineas de un pedido) y los dos LEFT JOIN LATERAL que enriquecen la pagina
            // del nivel 5. Como corren por fila de la pagina, este indice es lo que hace
            // que el enriquecimiento cueste 50 busquedas y no un barrido.
            migrationBuilder.CreateIndex(
                name: "ix_order_line_tenant_id_order_id",
                table: "order_line",
                columns: new[] { "tenant_id", "order_id" },
                filter: "is_active");

            // EXISTS del CTE (pedidos que contienen un producto) y agregacion de los niveles 3 y 4.
            // Termina en order_id para que el EXISTS se resuelva sin tocar la tabla.
            migrationBuilder.CreateIndex(
                name: "ix_order_line_tenant_id_product_id_order_id",
                table: "order_line",
                columns: new[] { "tenant_id", "product_id", "order_id" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_order_line_public_id",
                table: "order_line",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_category_id",
                table: "product",
                column: "category_id");

            // Nivel 4 (productos de una categoria), ya ordenado por nombre.
            migrationBuilder.CreateIndex(
                name: "ix_product_tenant_id_category_id_name",
                table: "product",
                columns: new[] { "tenant_id", "category_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_product_public_id",
                table: "product",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_tenant_id_sku",
                table: "product",
                columns: new[] { "tenant_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tenant_code",
                table: "tenant",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tenant_public_id",
                table: "tenant",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_line");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "customer_order");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "category");
        }
    }
}
