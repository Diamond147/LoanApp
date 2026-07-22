using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLoanTenureToLoanTenureInMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LoanTenure",
                table: "PreQualifiedLoans",
                newName: "LoanTenureInMonths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LoanTenureInMonths",
                table: "PreQualifiedLoans",
                newName: "LoanTenure");
        }
    }
}
