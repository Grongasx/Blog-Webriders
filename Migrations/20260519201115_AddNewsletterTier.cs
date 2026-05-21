using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThrottleBlog.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "NewsletterSubscribers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "NewsletterSubscribers",
                type: "text",
                nullable: false,
                defaultValue: "Common");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "NewsletterSubscribers");
        }
    }
}
