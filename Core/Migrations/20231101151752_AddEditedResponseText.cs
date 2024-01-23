using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aia_api.Migrations
{
    /// <inheritdoc />
    public partial class AddEditedResponseText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EditedResponseText",
                table: "Predictions",
                type: "TEXT",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedResponseText",
                table: "Predictions");
        }
    }
}
