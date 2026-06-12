using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Outlet.Cloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistryVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistryVisibility",
                table: "organizations",
                type: "text",
                nullable: false,
                defaultValue: "Private");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistryVisibility",
                table: "organizations");
        }
    }
}
