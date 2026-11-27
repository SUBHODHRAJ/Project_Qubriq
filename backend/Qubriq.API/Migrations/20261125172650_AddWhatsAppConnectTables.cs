using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppConnectTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatsAppConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Bsuid = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    ProfileName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    RecipientId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    AssignedDispatcherId = table.Column<int>(type: "int", nullable: true),
                    LastCustomerMessageAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppConversations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WhatsAppKnowledgeBases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Question = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "longtext", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppKnowledgeBases", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WhatsAppOutboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RecipientPhone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    MessageType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    TemplateName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppOutboxes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ConversationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    MetaMessageId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    Direction = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SenderType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    ToolInvoked = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "WhatsAppConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_Bsuid",
                table: "WhatsAppConversations",
                column: "Bsuid");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_PhoneNumber",
                table: "WhatsAppConversations",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_ConversationId",
                table: "WhatsAppMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_MetaMessageId",
                table: "WhatsAppMessages",
                column: "MetaMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppKnowledgeBases");

            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropTable(
                name: "WhatsAppOutboxes");

            migrationBuilder.DropTable(
                name: "WhatsAppConversations");
        }
    }
}
