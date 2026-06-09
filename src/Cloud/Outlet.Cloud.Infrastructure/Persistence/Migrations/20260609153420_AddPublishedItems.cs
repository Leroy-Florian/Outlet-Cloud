using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Outlet.Cloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "published_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ManifestJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "published_item_files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublishedItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_item_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_published_item_files_published_items_PublishedItemId",
                        column: x => x.PublishedItemId,
                        principalTable: "published_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_published_item_files_PublishedItemId",
                table: "published_item_files",
                column: "PublishedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_published_items_OrganizationId_Name",
                table: "published_items",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "published_item_files");

            migrationBuilder.DropTable(
                name: "published_items");
        }
    }
}
