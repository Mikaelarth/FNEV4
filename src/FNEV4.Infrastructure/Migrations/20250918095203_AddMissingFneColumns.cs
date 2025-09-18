using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNEV4.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingFneColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 9, 52, 1, 505, DateTimeKind.Utc).AddTicks(4778));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 9, 52, 1, 505, DateTimeKind.Utc).AddTicks(4824));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 9, 52, 1, 505, DateTimeKind.Utc).AddTicks(4838));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 0, 9, 14, 677, DateTimeKind.Utc).AddTicks(7390));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 0, 9, 14, 677, DateTimeKind.Utc).AddTicks(7396));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 0, 9, 14, 677, DateTimeKind.Utc).AddTicks(7400));
        }
    }
}
