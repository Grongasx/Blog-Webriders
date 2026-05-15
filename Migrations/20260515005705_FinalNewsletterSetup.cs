using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ThrottleBlog.Migrations
{
    /// <inheritdoc />
    public partial class FinalNewsletterSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.CreateTable(
                name: "NewsletterQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterQueue_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSendLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsletterQueueId = table.Column<int>(type: "int", nullable: false),
                    SubscriberId = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ResendMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSendLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterSendLog_NewsletterQueue_NewsletterQueueId",
                        column: x => x.NewsletterQueueId,
                        principalTable: "NewsletterQueue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsletterSendLog_NewsletterSubscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "NewsletterSubscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterQueue_PostId",
                table: "NewsletterQueue",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterQueue_Status",
                table: "NewsletterQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSendLog_NewsletterQueueId_SubscriberId",
                table: "NewsletterSendLog",
                columns: new[] { "NewsletterQueueId", "SubscriberId" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSendLog_SubscriberId",
                table: "NewsletterSendLog",
                column: "SubscriberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterSendLog");

            migrationBuilder.DropTable(
                name: "NewsletterQueue");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ImageUrl", "Name", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { 3, "https://images.unsplash.com/photo-1568772585407-9371f9bf3a87?w=400&q=70", "Custom", "custom", 3 },
                    { 5, "https://images.unsplash.com/photo-1591637333184-19aa84b3e01f?w=400&q=70", "Mecânica", "mecanica", 5 }
                });
        }
    }
}
