using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoteService.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OptionIndex = table.Column<int>(type: "int", nullable: false),
                    VoterToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_PollCode",
                table: "Votes",
                column: "PollCode");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_PollCode_VoterToken",
                table: "Votes",
                columns: new[] { "PollCode", "VoterToken" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");
        }
    }
}
