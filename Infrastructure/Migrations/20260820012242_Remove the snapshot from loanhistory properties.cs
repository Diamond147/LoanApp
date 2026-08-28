using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Removethesnapshotfromloanhistoryproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_Status",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "OutstandingAmountSnapshot",
                table: "LoanHistories",
                newName: "OutstandingAmount");

            migrationBuilder.RenameColumn(
                name: "InterestRateSnapshot",
                table: "LoanHistories",
                newName: "InterestRate");

            migrationBuilder.RenameColumn(
                name: "AccruedInterestSnapshot",
                table: "LoanHistories",
                newName: "AccruedInterest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutstandingAmount",
                table: "LoanHistories",
                newName: "OutstandingAmountSnapshot");

            migrationBuilder.RenameColumn(
                name: "InterestRate",
                table: "LoanHistories",
                newName: "InterestRateSnapshot");

            migrationBuilder.RenameColumn(
                name: "AccruedInterest",
                table: "LoanHistories",
                newName: "AccruedInterestSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status",
                table: "Loans",
                column: "Status");
        }
    }
}
