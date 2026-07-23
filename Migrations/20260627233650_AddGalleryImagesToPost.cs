using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThrottleBlog.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryImagesToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GalleryImages",
                table: "Posts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryImages",
                table: "Posts");
        }
    }
}
