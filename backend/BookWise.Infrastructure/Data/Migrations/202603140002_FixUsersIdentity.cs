using System;
using BookWise.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookWise.Infrastructure.Data.Migrations;

[DbContextAttribute(typeof(BookWiseDbContext))]
[Migration("202603140002_FixUsersIdentity")]
public partial class FixUsersIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SELECT setval(
                pg_get_serial_sequence('users', 'id'),
                (SELECT COALESCE(MAX(id), 1) FROM users)
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SELECT setval(
                pg_get_serial_sequence('users', 'id'),
                (SELECT COALESCE(MAX(id), 1) FROM users)
            );
            """);
    }
}

