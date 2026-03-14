using System;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookWise.Infrastructure.Data.Migrations;

[DbContextAttribute(typeof(BookWiseDbContext))]
[Migration("202603140001_UserScopedCatalog")]
public partial class UserScopedCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO users (id, email, name, google_subject, phone_number_e164, last_login_at, created_at, updated_at, is_active)
            OVERRIDING SYSTEM VALUE
            VALUES (1, 'system@local', 'System', NULL, NULL, NULL, NOW(), NULL, TRUE)
            ON CONFLICT (id) DO NOTHING;

            SELECT setval(
                pg_get_serial_sequence('users', 'id'),
                (SELECT COALESCE(MAX(id), 1) FROM users)
            );
            """);

        migrationBuilder.AddColumn<int>(
            name: "user_account_id",
            table: "authors",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "user_account_id",
            table: "genres",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "user_account_id",
            table: "books",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_authors_user_account_id",
            table: "authors",
            column: "user_account_id");

        migrationBuilder.CreateIndex(
            name: "IX_genres_user_account_id",
            table: "genres",
            column: "user_account_id");

        migrationBuilder.CreateIndex(
            name: "IX_books_user_account_id",
            table: "books",
            column: "user_account_id");

        migrationBuilder.AddForeignKey(
            name: "FK_authors_users_user_account_id",
            table: "authors",
            column: "user_account_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_genres_users_user_account_id",
            table: "genres",
            column: "user_account_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_books_users_user_account_id",
            table: "books",
            column: "user_account_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropIndex(name: "IX_genres_name", table: "genres");
        migrationBuilder.CreateIndex(
            name: "IX_genres_user_account_id_name",
            table: "genres",
            columns: ["user_account_id", "name"],
            unique: true);

        migrationBuilder.DropIndex(name: "IX_books_isbn", table: "books");
        migrationBuilder.CreateIndex(
            name: "IX_books_user_account_id_isbn",
            table: "books",
            columns: ["user_account_id", "isbn"],
            unique: true,
            filter: "\"isbn\" IS NOT NULL");

        migrationBuilder.DropIndex(name: "IX_books_title", table: "books");
        migrationBuilder.CreateIndex(
            name: "IX_books_user_account_id_title",
            table: "books",
            columns: ["user_account_id", "title"]);

        migrationBuilder.DropIndex(name: "IX_authors_name", table: "authors");
        migrationBuilder.CreateIndex(
            name: "IX_authors_user_account_id_name",
            table: "authors",
            columns: ["user_account_id", "name"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_authors_users_user_account_id", table: "authors");
        migrationBuilder.DropForeignKey(name: "FK_genres_users_user_account_id", table: "genres");
        migrationBuilder.DropForeignKey(name: "FK_books_users_user_account_id", table: "books");

        migrationBuilder.DropIndex(name: "IX_authors_user_account_id", table: "authors");
        migrationBuilder.DropIndex(name: "IX_genres_user_account_id", table: "genres");
        migrationBuilder.DropIndex(name: "IX_books_user_account_id", table: "books");

        migrationBuilder.DropIndex(name: "IX_genres_user_account_id_name", table: "genres");
        migrationBuilder.DropIndex(name: "IX_books_user_account_id_isbn", table: "books");
        migrationBuilder.DropIndex(name: "IX_books_user_account_id_title", table: "books");
        migrationBuilder.DropIndex(name: "IX_authors_user_account_id_name", table: "authors");

        migrationBuilder.CreateIndex(
            name: "IX_genres_name",
            table: "genres",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_books_isbn",
            table: "books",
            column: "isbn",
            unique: true,
            filter: "\"isbn\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_books_title",
            table: "books",
            column: "title");

        migrationBuilder.CreateIndex(
            name: "IX_authors_name",
            table: "authors",
            column: "name");

        migrationBuilder.DropColumn(name: "user_account_id", table: "authors");
        migrationBuilder.DropColumn(name: "user_account_id", table: "genres");
        migrationBuilder.DropColumn(name: "user_account_id", table: "books");

        migrationBuilder.Sql("DELETE FROM users WHERE id = 1 AND email = 'system@local';");
    }
}
