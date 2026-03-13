using System;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookWise.Infrastructure.Data.Migrations;

[DbContextAttribute(typeof(BookWiseDbContext))]
[Migration("202603130002_AddAuth")]
public partial class AddAuth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                google_subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                phone_number_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "login_otps",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                phone_number_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                pilot_message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_login_otps", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_users_email",
            table: "users",
            column: "email",
            filter: "\"email\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_users_google_subject",
            table: "users",
            column: "google_subject",
            unique: true,
            filter: "\"google_subject\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_users_phone_number_e164",
            table: "users",
            column: "phone_number_e164",
            unique: true,
            filter: "\"phone_number_e164\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_login_otps_expires_at",
            table: "login_otps",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "IX_login_otps_phone_number_e164",
            table: "login_otps",
            column: "phone_number_e164");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "login_otps");

        migrationBuilder.DropTable(
            name: "users");
    }
}
