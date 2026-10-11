using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityAndParticipantLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LimitLabel",
                table: "Games",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantLimitOverride",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommunityRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OrganizerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRequests_OrganizerId",
                table: "CommunityRequests",
                column: "OrganizerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityRequests");

            migrationBuilder.DropColumn(
                name: "LimitLabel",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ParticipantLimitOverride",
                table: "Games");
        }
    }
}
