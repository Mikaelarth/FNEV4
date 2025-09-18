using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNEV4.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFneEnrichedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FneCertificationDate",
                table: "FneInvoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneCertificationNumber",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneCertifiedInvoiceDetails",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneCompanyNcc",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneDigitalSignature",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneDownloadUrl",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FneHasWarning",
                table: "FneInvoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FneInvoiceId",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FnePublicVerificationToken",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneQrCode",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FneStickerBalance",
                table: "FneInvoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneValidationUrl",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FneWarningMessage",
                table: "FneInvoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCertified",
                table: "FneInvoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FneCertificationDate",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneCertificationNumber",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneCertifiedInvoiceDetails",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneCompanyNcc",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneDigitalSignature",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneDownloadUrl",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneHasWarning",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneInvoiceId",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FnePublicVerificationToken",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneQrCode",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneStickerBalance",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneValidationUrl",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "FneWarningMessage",
                table: "FneInvoices");

            migrationBuilder.DropColumn(
                name: "IsCertified",
                table: "FneInvoices");

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 15, 11, 1, 31, 717, DateTimeKind.Utc).AddTicks(7343));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 15, 11, 1, 31, 717, DateTimeKind.Utc).AddTicks(7350));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 15, 11, 1, 31, 717, DateTimeKind.Utc).AddTicks(7368));
        }
    }
}
