using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssignmentId1FromAssignmentStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentStudents_Assignments_AssignmentId1",
                table: "AssignmentStudents");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentStudents_AssignmentId1",
                table: "AssignmentStudents");

            migrationBuilder.DropColumn(
                name: "AssignmentId1",
                table: "AssignmentStudents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentId1",
                table: "AssignmentStudents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentStudents_AssignmentId1",
                table: "AssignmentStudents",
                column: "AssignmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentStudents_Assignments_AssignmentId1",
                table: "AssignmentStudents",
                column: "AssignmentId1",
                principalTable: "Assignments",
                principalColumn: "Id");
        }
    }
}
