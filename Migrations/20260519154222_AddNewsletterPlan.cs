using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThrottleBlog.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "NewsletterSubscribers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                table: "NewsletterSubscribers");
        }
    }
}
