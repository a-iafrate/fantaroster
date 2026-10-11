using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameOrganizer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "Games",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // Games created before ownership existed go to the first organizer (by user name), as agreed with the owner of the
            // project. If there are no organizers yet nothing changes and the games stay without an owner.
            migrationBuilder.Sql(
                """
                UPDATE [Games]
                SET [OrganizerId] = (SELECT TOP (1) [Id] FROM [AspNetUsers] ORDER BY [NormalizedUserName])
                WHERE [OrganizerId] IS NULL AND EXISTS (SELECT 1 FROM [AspNetUsers])
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Games_OrganizerId",
                table: "Games",
                column: "OrganizerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_OrganizerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Games");
        }
    }
}
