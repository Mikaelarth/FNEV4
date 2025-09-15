using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FNEV4.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DefaultPaymentMethod",
                table: "Clients",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "cash",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPointOfSale",
                table: "Clients",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerNcc = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    CustomerTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerRealName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PointOfSale = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreditNoteReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalAmountHT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountTTC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FneTransactionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FneCertificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FneStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FneResponseMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SourceSheetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    AmountHT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountTTC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Clients_DefaultPaymentMethod",
                table: "Clients",
                column: "DefaultPaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Clients_DefaultPaymentMethod",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DefaultPointOfSale",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultPaymentMethod",
                table: "Clients",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldDefaultValue: "cash");

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 7, 1, 22, 38, 245, DateTimeKind.Utc).AddTicks(1701));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 7, 1, 22, 38, 245, DateTimeKind.Utc).AddTicks(1721));

            migrationBuilder.UpdateData(
                table: "VatTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 7, 1, 22, 38, 245, DateTimeKind.Utc).AddTicks(1733));
        }
    }
}
