using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRegistrationPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRegistrationPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PublicRegistrationEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BookingCustomerSignupEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FinalRecipientSignupEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequireAdminApproval = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActivatePublicAccountsImmediately = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AdminAccountCreationEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRegistrationPolicies", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "AccountRegistrationPolicies",
                columns: new[] { "Id", "ActivatePublicAccountsImmediately", "AdminAccountCreationEnabled", "BookingCustomerSignupEnabled", "FinalRecipientSignupEnabled", "PublicRegistrationEnabled", "RequireAdminApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { 1, true, true, true, false, true, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRegistrationPolicies");
        }
    }
}
