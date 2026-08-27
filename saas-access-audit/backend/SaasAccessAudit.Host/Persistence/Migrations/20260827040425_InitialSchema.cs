using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SaasAccessAudit.Host.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_denial_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    route = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    attempted_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    attempted_by_display = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    enforcement_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    blocked = table.Column<bool>(type: "boolean", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_denial_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "action_audit_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    action_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    performed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    performed_by_display = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    authorized_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    external_effect_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    external_effect_applied = table.Column<bool>(type: "boolean", nullable: true),
                    external_effect_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_action_audit_entry", x => x.id);
                    table.CheckConstraint("ck_action_audit_entry_reason", "length(btrim(reason)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deactivated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_user_group",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    group_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_role_mapping",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    group_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_role_mapping", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_destructive = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => x.id);
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

            migrationBuilder.CreateIndex(
                name: "ix_access_denial_entry_tenant_permission",
                table: "access_denial_entry",
                columns: new[] { "tenant_id", "permission_code", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_access_denial_entry_tenant_recent",
                table: "access_denial_entry",
                columns: new[] { "tenant_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_denial_entry_would_have_blocked",
                table: "access_denial_entry",
                columns: new[] { "tenant_id", "occurred_at_utc" },
                filter: "blocked = false");

            migrationBuilder.CreateIndex(
                name: "ux_access_denial_entry_public_id",
                table: "access_denial_entry",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_action_audit_entry_pending_external_effect",
                table: "action_audit_entry",
                columns: new[] { "tenant_id", "occurred_at_utc" },
                filter: "external_effect_applied = false");

            migrationBuilder.CreateIndex(
                name: "ix_action_audit_entry_tenant_action",
                table: "action_audit_entry",
                columns: new[] { "tenant_id", "action_code", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_action_audit_entry_tenant_recent",
                table: "action_audit_entry",
                columns: new[] { "tenant_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_action_audit_entry_tenant_subject",
                table: "action_audit_entry",
                columns: new[] { "tenant_id", "subject_key", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_action_audit_entry_public_id",
                table: "action_audit_entry",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_tenant_id_is_active_username",
                table: "app_user",
                columns: new[] { "tenant_id", "is_active", "username" });

            migrationBuilder.CreateIndex(
                name: "ux_app_user_public_id",
                table: "app_user",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_app_user_tenant_id_username",
                table: "app_user",
                columns: new[] { "tenant_id", "username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_app_user_group_public_id",
                table: "app_user_group",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_app_user_group_tenant_id_user_id_group_name",
                table: "app_user_group",
                columns: new[] { "tenant_id", "user_id", "group_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_role_mapping_tenant_id_group_name",
                table: "group_role_mapping",
                columns: new[] { "tenant_id", "group_name" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_group_role_mapping_public_id",
                table: "group_role_mapping",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_group_role_mapping_tenant_id_group_name_role_id",
                table: "group_role_mapping",
                columns: new[] { "tenant_id", "group_name", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_permission_code",
                table: "permission",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_permission_public_id",
                table: "permission",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_role_public_id",
                table: "role",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_role_tenant_id_code",
                table: "role",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_tenant_id_role_id",
                table: "role_permission",
                columns: new[] { "tenant_id", "role_id" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_role_permission_public_id",
                table: "role_permission",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_role_permission_tenant_id_role_id_permission_id",
                table: "role_permission",
                columns: new[] { "tenant_id", "role_id", "permission_id" },
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

            // ---------------------------------------------------------------------------------
            // APPEND-ONLY, SOSTENIDO POR EL MOTOR
            //
            // Las dos tablas de bitacora se insertan y nunca se editan ni se borran. Que el
            // codigo no tenga metodos para hacerlo es una promesa; esto es una garantia. El
            // disparador hace fallar cualquier UPDATE o DELETE, venga de un repositorio nuevo que
            // alguien escriba manana, de una migracion, o de una consola de administracion.
            //
            // Falla RUIDOSAMENTE (RAISE EXCEPTION) y no en silencio con una regla DO INSTEAD
            // NOTHING: un borrado que parece funcionar y no hace nada es peor que uno que truena,
            // porque nadie se entera de que la bitacora quedo intacta cuando esperaba otra cosa.
            // ---------------------------------------------------------------------------------
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION audit_append_only() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION
                        'La tabla % es append-only: no admite % (bitacora de acciones sensibles).',
                        TG_TABLE_NAME, TG_OP;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_action_audit_entry_append_only
                BEFORE UPDATE OR DELETE ON action_audit_entry
                FOR EACH ROW EXECUTE FUNCTION audit_append_only();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_access_denial_entry_append_only
                BEFORE UPDATE OR DELETE ON access_denial_entry
                FOR EACH ROW EXECUTE FUNCTION audit_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_access_denial_entry_append_only ON access_denial_entry;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_action_audit_entry_append_only ON action_audit_entry;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit_append_only();");

            migrationBuilder.DropTable(
                name: "access_denial_entry");

            migrationBuilder.DropTable(
                name: "action_audit_entry");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "app_user_group");

            migrationBuilder.DropTable(
                name: "group_role_mapping");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "tenant");
        }
    }
}
