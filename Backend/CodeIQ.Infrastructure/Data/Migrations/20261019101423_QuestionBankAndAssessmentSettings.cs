using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeIQ.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuestionBankAndAssessmentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceQuestionBankItemId",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCodeRun",
                table: "Assessments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CandidateInstructions",
                table: "Assessments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                table: "Assessments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "QuestionBankItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    ProblemStatement = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultLanguage = table.Column<int>(type: "INTEGER", nullable: false),
                    StarterCode = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBankItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionBankTestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionBankItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Input = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "TEXT", nullable: false),
                    IsSample = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBankTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionBankTestCases_QuestionBankItems_QuestionBankItemId",
                        column: x => x.QuestionBankItemId,
                        principalTable: "QuestionBankItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBankItems_Slug",
                table: "QuestionBankItems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBankTestCases_QuestionBankItemId",
                table: "QuestionBankTestCases",
                column: "QuestionBankItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionBankTestCases");

            migrationBuilder.DropTable(
                name: "QuestionBankItems");

            migrationBuilder.DropColumn(
                name: "SourceQuestionBankItemId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AllowCodeRun",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "CandidateInstructions",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                table: "Assessments");
        }
    }
}
