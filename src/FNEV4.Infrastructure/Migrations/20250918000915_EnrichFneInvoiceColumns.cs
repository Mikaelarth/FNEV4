using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNEV4.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrichFneInvoiceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FneBalanceSticker",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneCertificationHash",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FneCertificationTimestamp",
                table: "FneInvoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneProcessingStatus",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneQrCodeData",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FneBalanceSticker",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneCertificationHash",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneCertificationTimestamp",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneProcessingStatus",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneQrCodeData",
                table: "FneInvoices");

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 23, 59, 5, 275, DateTimeKind.Utc).AddTicks(6265));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 23, 59, 5, 275, DateTimeKind.Utc).AddTicks(6276));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 17, 23, 59, 5, 275, DateTimeKind.Utc).AddTicks(6284));
        }
    }
}
