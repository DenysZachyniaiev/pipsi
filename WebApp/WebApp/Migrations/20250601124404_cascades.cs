using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class cascades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_SubjectId",
                table: "ScheduleEntries",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleEntries_Subjects_SubjectId",
                table: "ScheduleEntries",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleEntries_Subjects_SubjectId",
                table: "ScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleEntries_SubjectId",
                table: "ScheduleEntries");
        }
    }
}
