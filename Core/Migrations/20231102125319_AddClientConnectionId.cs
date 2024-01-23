using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aia_api.Migrations
{
    /// <inheritdoc />
    public partial class AddClientConnectionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientConnectionId",
                table: "Predictions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientConnectionId",
                table: "Predictions");
        }
    }
}
