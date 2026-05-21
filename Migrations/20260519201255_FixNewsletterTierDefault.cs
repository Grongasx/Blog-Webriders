using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThrottleBlog.Migrations
{
    /// <inheritdoc />
    public partial class FixNewsletterTierDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """UPDATE "NewsletterSubscribers" SET "Tier" = 'Common' WHERE "Tier" = '' OR "Tier" IS NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
