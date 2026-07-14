using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixLoanHistoryRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanHistories_Loans_LoanId1",
                table: "LoanHistories");

            migrationBuilder.DropIndex(
                name: "IX_LoanHistories_LoanId1",
                table: "LoanHistories");

            migrationBuilder.DropColumn(
                name: "LoanId1",
                table: "LoanHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoanId1",
                table: "LoanHistories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanHistories_LoanId1",
                table: "LoanHistories",
                column: "LoanId1");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanHistories_Loans_LoanId1",
                table: "LoanHistories",
                column: "LoanId1",
                principalTable: "Loans",
                principalColumn: "Id");
        }
    }
}
