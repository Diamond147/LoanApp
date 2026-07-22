using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMultipleThings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "LoanHistories");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Loans",
                newName: "RequestedAmount");

            migrationBuilder.RenameColumn(
                name: "ApprovalDate",
                table: "LoanHistories",
                newName: "UpdatedDate");

            //migrationBuilder.AlterColumn<int>(
            //    name: "LoanTenure",
            //    table: "PreQualifiedLoans",
            //    type: "integer",
            //    nullable: false,
            //    oldClrType: typeof(string),
            //    oldType: "text");

            migrationBuilder.Sql(@"
                ALTER TABLE ""PreQualifiedLoans"" 
                ALTER COLUMN ""LoanTenure"" TYPE integer 
                USING (
                    -- Extract only the digits from the string (e.g., '12 months' -> '12') and cast to int
                    NULLIF(regexp_replace(""LoanTenure"", '\D', '', 'g'), '')::integer
                );
            ");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PreQualifiedLoans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PreQualifiedLoans");

            migrationBuilder.RenameColumn(
                name: "RequestedAmount",
                table: "Loans",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "LoanHistories",
                newName: "ApprovalDate");

            migrationBuilder.AlterColumn<string>(
                name: "LoanTenure",
                table: "PreQualifiedLoans",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "Loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "LoanHistories",
                type: "numeric",
                nullable: true);
        }
    }
}
