using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserProfileId = table.Column<string>(type: "text", nullable: true),
                    EmailAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    EmailType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LoanId = table.Column<string>(type: "text", nullable: true),
                    UserProfileId = table.Column<string>(type: "text", nullable: true),
                    PaystackReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaystackResponse = table.Column<string>(type: "text", nullable: true),
                    AuthorizationUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreQualifiedLoans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LoanType = table.Column<string>(type: "text", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LoanTenure = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreQualifiedLoans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LoanType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserProfileId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LoanId = table.Column<string>(type: "text", nullable: false),
                    LoanType = table.Column<string>(type: "text", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UserProfileId = table.Column<string>(type: "text", nullable: false),
                    LoanId1 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanHistories_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanHistories_Loans_LoanId1",
                        column: x => x.LoanId1,
                        principalTable: "Loans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_EmailType",
                table: "EmailLogs",
                column: "EmailType");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentDate",
                table: "EmailLogs",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_UserProfileId",
                table: "EmailLogs",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanHistories_LoanId",
                table: "LoanHistories",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanHistories_LoanId1",
                table: "LoanHistories",
                column: "LoanId1");

            migrationBuilder.CreateIndex(
                name: "IX_LoanHistories_UserProfileId",
                table: "LoanHistories",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status",
                table: "Loans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_UserProfileId",
                table: "Loans",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LoanId",
                table: "Payments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaystackReference",
                table: "Payments",
                column: "PaystackReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserProfileId",
                table: "Payments",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PreQualifiedLoans_LoanType",
                table: "PreQualifiedLoans",
                column: "LoanType");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "LoanHistories");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PreQualifiedLoans");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
