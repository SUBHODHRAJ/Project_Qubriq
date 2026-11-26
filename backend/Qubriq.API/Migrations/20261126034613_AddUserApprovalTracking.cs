using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApprovalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresAdminApproval",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresAdminApproval",
                table: "Users");
        }
    }
}
