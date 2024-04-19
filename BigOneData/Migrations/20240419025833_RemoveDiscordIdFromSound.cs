using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigOneData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDiscordIdFromSound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUserId",
                table: "Sounds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordUserId",
                table: "Sounds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
