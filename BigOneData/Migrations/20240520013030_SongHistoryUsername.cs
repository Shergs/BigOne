using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigOneData.Migrations
{
    /// <inheritdoc />
    public partial class SongHistoryUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordUsername",
                table: "SongHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUsername",
                table: "SongHistory");
        }
    }
}
