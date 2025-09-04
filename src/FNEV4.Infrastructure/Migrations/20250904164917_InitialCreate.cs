using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FNEV4.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ClientNcc = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClientType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DefaultTemplate = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DefaultCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    SellerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TaxIdentificationNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ncc = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ShareCapital = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RccmNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PointsOfSale = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultPointOfSale = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsValidated = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StickerBalance = table.Column<int>(type: "INTEGER", nullable: false),
                    StickerAlertThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultInvoiceSettings = table.Column<string>(type: "TEXT", nullable: true),
                    Logo = table.Column<string>(type: "TEXT", nullable: true),
                    ElectronicSignature = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FneConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WebUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BearerToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsValidatedByDgi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupportEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequestTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetryAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiEndpoints = table.Column<string>(type: "TEXT", nullable: true),
                    ApiVersion = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    SslCertificates = table.Column<string>(type: "TEXT", nullable: true),
                    LastConnectivityTest = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastConnectivityResult = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastTestErrorMessages = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedSpecimens = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FneConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalInvoicesFound = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoicesImported = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessages = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FneInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FneReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InvoiceType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PointOfSale = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Establishment = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Template = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    TotalAmountHT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountTTC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GlobalDiscount = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    VerificationToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    VerificationUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ParentInvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RneNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsRne = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommercialMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Footer = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ForeignCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ForeignCurrencyRate = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ImportSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CertifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessages = table.Column<string>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FneInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FneInvoices_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FneInvoices_FneInvoices_ParentInvoiceId",
                        column: x => x.ParentInvoiceId,
                        principalTable: "FneInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FneInvoices_ImportSessions_ImportSessionId",
                        column: x => x.ImportSessionId,
                        principalTable: "ImportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FneApiLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FneInvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    RequestHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FneReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VerificationToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StickerBalance = table.Column<int>(type: "INTEGER", nullable: true),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ServerIpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LogLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FneApiLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FneApiLogs_FneInvoices_FneInvoiceId",
                        column: x => x.FneInvoiceId,
                        principalTable: "FneInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FneInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FneInvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    MeasurementUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VatTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VatCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LineAmountHT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineVatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmountTTC = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItemDiscount = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LineOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    FneItemId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CustomTaxes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FneInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FneInvoiceItems_FneInvoices_FneInvoiceId",
                        column: x => x.FneInvoiceId,
                        principalTable: "FneInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FneInvoiceItems_VatTypes_VatTypeId",
                        column: x => x.VatTypeId,
                        principalTable: "VatTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "FneConfigurations",
                columns: new[] { "Id", "ApiEndpoints", "ApiKey", "ApiVersion", "BaseUrl", "BearerToken", "ConfigurationName", "CreatedAt", "CreatedBy", "CreatedDate", "Environment", "IsActive", "IsDeleted", "IsValidatedByDgi", "LastConnectivityResult", "LastConnectivityTest", "LastModifiedDate", "LastTestErrorMessages", "MaxRetryAttempts", "ModifiedBy", "Notes", "RequestTimeoutSeconds", "RetryDelaySeconds", "SslCertificates", "SubmittedSpecimens", "SupportEmail", "UpdatedAt", "ValidationDate", "WebUrl" },
                values: new object[] { new Guid("99999999-9999-9999-9999-999999999999"), null, null, "1.0", "http://54.247.95.108/ws", null, "Test DGI", new DateTime(2025, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2025, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Test", true, false, false, null, null, null, null, 3, null, "Configuration par défaut pour l'environnement de test DGI", 30, 5, null, null, "support.fne@dgi.gouv.ci", null, null, "http://54.247.95.108" });

            migrationBuilder.InsertData(
                table: "VatTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "IsDeleted", "Rate", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "TVA", new DateTime(2025, 9, 4, 16, 49, 16, 299, DateTimeKind.Utc).AddTicks(7402), "TVA normal de 18%", true, false, 18.00m, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "TVAB", new DateTime(2025, 9, 4, 16, 49, 16, 299, DateTimeKind.Utc).AddTicks(7428), "TVA réduit de 9%", true, false, 9.00m, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "TVAC", new DateTime(2025, 9, 4, 16, 49, 16, 299, DateTimeKind.Utc).AddTicks(7434), "TVA exec conv de 0%", true, false, 0.00m, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "TVAD", new DateTime(2025, 9, 4, 0, 0, 0, 0, DateTimeKind.Utc), "TVA exec leg de 0% pour TEE et RME", true, false, 0.00m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientCode",
                table: "Clients",
                column: "ClientCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientNcc",
                table: "Clients",
                column: "ClientNcc");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientType",
                table: "Clients",
                column: "ClientType");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IsActive",
                table: "Clients",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Name",
                table: "Clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Ncc",
                table: "Companies",
                column: "Ncc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FneApiLogs_FneInvoiceId",
                table: "FneApiLogs",
                column: "FneInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FneApiLogs_IsSuccess",
                table: "FneApiLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_FneApiLogs_LogLevel",
                table: "FneApiLogs",
                column: "LogLevel");

            migrationBuilder.CreateIndex(
                name: "IX_FneApiLogs_OperationType",
                table: "FneApiLogs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_FneApiLogs_Timestamp",
                table: "FneApiLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_FneConfigurations_ConfigurationName_Environment",
                table: "FneConfigurations",
                columns: new[] { "ConfigurationName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FneConfigurations_Environment",
                table: "FneConfigurations",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_FneConfigurations_IsActive",
                table: "FneConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FneConfigurations_IsValidatedByDgi",
                table: "FneConfigurations",
                column: "IsValidatedByDgi");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoiceItems_FneInvoiceId",
                table: "FneInvoiceItems",
                column: "FneInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoiceItems_ProductCode",
                table: "FneInvoiceItems",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoiceItems_VatTypeId",
                table: "FneInvoiceItems",
                column: "VatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_ClientId",
                table: "FneInvoices",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_FneReference",
                table: "FneInvoices",
                column: "FneReference");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_ImportSessionId",
                table: "FneInvoices",
                column: "ImportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_InvoiceDate",
                table: "FneInvoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_InvoiceNumber",
                table: "FneInvoices",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_ParentInvoiceId",
                table: "FneInvoices",
                column: "ParentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FneInvoices_Status",
                table: "FneInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessions_StartedAt",
                table: "ImportSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessions_Status",
                table: "ImportSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSessions_UserName",
                table: "ImportSessions",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_VatTypes_Code",
                table: "VatTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VatTypes_IsActive",
                table: "VatTypes",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "FneApiLogs");

            migrationBuilder.DropTable(
                name: "FneConfigurations");

            migrationBuilder.DropTable(
                name: "FneInvoiceItems");

            migrationBuilder.DropTable(
                name: "FneInvoices");

            migrationBuilder.DropTable(
                name: "VatTypes");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "ImportSessions");
        }
    }
}
