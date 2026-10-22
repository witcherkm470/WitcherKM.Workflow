using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitcherKM.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExternalImageCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalImages",
                columns: table => new
                {
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    LocalUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalImages", x => x.SourceUrl);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalImages_LocalUrl",
                table: "ExternalImages",
                column: "LocalUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalImages");
        }
    }
}
