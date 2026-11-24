using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridRbacAndRecipientAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientType",
                table: "Users",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Drivers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Customers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeliveryProofs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    SubmittedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ProofType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    FileReference = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryProofs_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryProofs_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ProviderSubject = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Module = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShipmentRecipients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentRecipients_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentRecipients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    IsAllowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryProofs_ShipmentId",
                table: "DeliveryProofs",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryProofs_SubmittedByUserId",
                table: "DeliveryProofs",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderSubject",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRecipients_ShipmentId_UserId",
                table: "ShipmentRecipients",
                columns: new[] { "ShipmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRecipients_UserId",
                table: "ShipmentRecipients",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Users_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Users_UserId",
                table: "Drivers");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeliveryProofs");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "ShipmentRecipients");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecipientType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
