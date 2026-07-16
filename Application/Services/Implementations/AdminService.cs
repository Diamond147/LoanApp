using Application.DTOs;
using Application.Exceptions;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Admin;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;
using Domain.Enums;
using System.Net;

namespace Application.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;

        private const decimal MinLoanAmount = 1000;
        private const decimal MaxLoanAmount = 10000;

        public AdminService(IAdminRepository adminRepository, IUserRepository userRepository, IEmailService emailService)
        {
            _adminRepository=adminRepository;
            _userRepository=userRepository;
            _emailService=emailService;
        }

        // Dashboard
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var result = await _adminRepository.GetDashboardStatsAsync();
                if (result == null)
                    throw new NotFoundException("Dashboard details not found.");
                return result;
            }
            catch (Exception ex) when (ex is NotFoundException) 
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<ContinuationResponse<AdminUserDetailDto>> GetAllUsersDetailsAsync(
            int pageSize, string? continuationToken, string? userId, string? email, string? mobileNumber, string? gender, string? nationality, string? searchTerm)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ValidationException("PageSize must be between 1 and 100.");
                }

                // Get users with their loans
                var (users, nextToken) = await _adminRepository.GetAllUsersDetailsAsync(pageSize, continuationToken, userId, email, mobileNumber, gender, nationality, searchTerm);
                if (!users.Any())
                {
                    return new ContinuationResponse<AdminUserDetailDto>
                    {
                        Data = new List<AdminUserDetailDto>(),
                        ContinuationToken = null,
                        HasMore = false
                    };
                }

                var userDetails = new List<AdminUserDetailDto>();

                foreach (var user in users)
                {
                    // Get all histories from all loans for this user
                    var histories = user.Loans?
                        .SelectMany(l => l.LoanHistories ?? new List<LoanHistory>())
                        .ToList() ?? new List<LoanHistory>();

                    var userDetail = new AdminUserDetailDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        MobileNumber = user.MobileNumber,
                        Gender = user.Gender,
                        DateOfBirth = user.DateOfBirth,
                        Nationality = user.Nationality,
                        SignUpDate = user.SignUpDate,

                        // Map loans
                        Loans = user.Loans?.Select(l => new AdminLoanDto
                        {
                            Id = l.Id,
                            LoanType = l.LoanType,
                            Amount = l.Amount,
                            ApprovedAmount = l.ApprovedAmount,
                            Status = l.Status,
                            RequestedDate = l.RequestedDate,
                            ApprovalDate = l.ApprovalDate,
                            UserProfileId = l.UserProfileId,
                            UserName = $"{user.FirstName} {user.LastName}"
                        })
                            .OrderByDescending(l => l.RequestedDate)
                            .ToList() ?? new List<AdminLoanDto>(),

                        // Map loan histories
                        LoanHistories = (histories ?? Enumerable.Empty<LoanHistory>())
                        .Select(h => new AdminLoanHistoryDto
                        {
                            Id = h.Id,
                            LoanId = h.LoanId,
                            LoanType = h.LoanType,
                            RequestedAmount = h.RequestedAmount,
                            ApprovedAmount = h.ApprovedAmount,
                            RequestedDate = h.RequestedDate,
                            ApprovalDate = h.ApprovalDate,
                            Status = h.Status,
                            UserProfileId = h.UserProfileId,
                        }).OrderByDescending(h => h.RequestedDate)
                        .ToList() ?? new List<AdminLoanHistoryDto>(),
                    };
                    userDetails.Add(userDetail);
                }
                return new ContinuationResponse<AdminUserDetailDto>
                {
                    Data= userDetails,
                    ContinuationToken = nextToken,
                    HasMore = nextToken != null,
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // User Management
        public async Task<ContinuationResponse<AdminUserDto>> GetUsersWithContinuationAsync(int pageSize, string? continuationToken, string? userId)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ValidationException("Page size must be between 1 and 100.");
                }
                var (users, newContinuationToken) = await _adminRepository.GetUsersWithContinuationAsync(pageSize, continuationToken, userId);

                var userDtos = new List<AdminUserDto>();

                foreach (var user in users)
                {
                    var newUser = await _adminRepository.GetUserByIdAsync(user.Id);

                    userDtos.Add(new AdminUserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Gender = user.Gender,
                        DateOfBirth = user.DateOfBirth,
                        MobileNumber = user.MobileNumber,
                        SignUpDate = user.SignUpDate,
                        Nationality = user.Nationality,
                    });
                }
                return new ContinuationResponse<AdminUserDto>
                {
                    Data = userDtos,
                    ContinuationToken = newContinuationToken,
                    HasMore = !string.IsNullOrEmpty(newContinuationToken)
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<AdminUserDto?> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _adminRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new NotFoundException("User not found");

                return new AdminUserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth,
                    MobileNumber = user.MobileNumber,
                    SignUpDate = user.SignUpDate,
                    Nationality = user.Nationality
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        // Changing user role to either "Admin" or "User"
        public async Task ChangeUserRoleAsync(string UserId, ChangeRoleDto dto)
        {
            // Input Validation
            if (string.IsNullOrEmpty(dto.NewRole))
            {
                throw new ArgumentException("Role cannot be empty.");
            }

            // Normalize role input to ensure case-insensitivity
            var normalizedRole = char.ToUpper(dto.NewRole[0]) + dto.NewRole.Substring(1).ToLower();
            if (normalizedRole != "Admin" && normalizedRole != "User")
            {
                throw new ArgumentException("Invalid role. Allowed roles are 'Admin' or 'User'.");
            }

            // Fetch User
            var user = await _userRepository.GetUserByIdAsync(UserId);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {UserId} not found.");
            }

            user.Role = normalizedRole;
            await _userRepository.UpdateUserAsync(user);
        }


        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var deleted =  await _adminRepository.DeleteUserAsync(userId);
                if (!deleted)
                {
                    throw new NotFoundException("User not found.");
                }
                return deleted;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // Loan Management
        public async Task<ContinuationResponse<AdminLoanDto>> GetLoansWithContinuationAsync(int pageSize, string? continuationToken, LoanStatus? status, string? loanId)
        {
            try
            {
                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ValidationException("PageSize must be between 1 and 100");
                }
                var (loans, newContinuationToken) = await _adminRepository.GetLoansWithContinuationAsync(pageSize, continuationToken, status, loanId);

                var loanDtos = new List<AdminLoanDto>();

                foreach (var loan in loans)
                {
                    var user = await _adminRepository.GetUserByIdAsync(loan.UserProfileId);
                    loanDtos.Add(new AdminLoanDto
                    {
                        Id = loan.Id,
                        LoanType = loan.LoanType,
                        Amount = loan.Amount,
                        ApprovedAmount = loan.ApprovedAmount,
                        Status = loan.Status,
                        RequestedDate = loan.RequestedDate,
                        ApprovalDate = loan.ApprovalDate,
                        UserProfileId = loan.UserProfileId,
                        UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                    });
                }
                return new ContinuationResponse<AdminLoanDto>
                {
                    Data = loanDtos,
                    ContinuationToken = newContinuationToken,
                    HasMore = !string.IsNullOrEmpty(newContinuationToken)
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        //public async Task<bool> MarkLoanAsPaidAsync(string loanId)
        //{
        //    var loan = await _adminRepository.GetLoanByIdAsync(loanId);
        //    if (loan == null)
        //        return false;

        //    if (loan.Status != LoanStatus.Approved)
        //        throw new InvalidOperationException("Only approved loans can be marked as paid.");

        //    loan.Status = LoanStatus.Paid;
        //    loan.UpdatedDate = DateTime.UtcNow;

        //    var history = new LoanHistory
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        LoanId = loan.Id,
        //        LoanType = loan.LoanType,
        //        RequestedAmount = loan.Amount,
        //        ApprovedAmount = loan.ApprovedAmount,
        //        RequestedDate = loan.RequestedDate,
        //        ApprovalDate = loan.ApprovalDate,
        //        Status = LoanStatus.Paid,
        //        UserProfileId = loan.UserProfileId,
        //    };
        //    await _adminRepository.UpdateLoanStatusAsync(loanId, LoanStatus.Paid);
        //    await _adminRepository.AddLoanHistoryAsync(history);

        //    return true;
        //}

        public async Task<AdminLoanDto?> GetLoanByIdAsync(string loanId)
        {
            try
            {
                var loan = await _adminRepository.GetLoanByIdAsync(loanId);
                if (loan == null)
                    throw new NotFoundException("Loan not found");

                var user = await _adminRepository.GetUserByIdAsync(loan.UserProfileId);

                return new AdminLoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    Amount = loan.Amount,
                    ApprovedAmount = loan.ApprovedAmount,
                    Status = loan.Status,
                    RequestedDate = loan.RequestedDate,
                    ApprovalDate = loan.ApprovalDate,
                    UserProfileId = loan.UserProfileId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<AdminLoanDto?> UpdateLoanStatusAsync(string loanId, LoanStatus newStatus)
        {
            try
            {
                var loan = await _adminRepository.UpdateLoanStatusAsync(loanId, newStatus);
                if (loan == null)
                {
                    throw new NotFoundException( "Loan not found" );
                }

                var user = await _userRepository.GetUserByIdAsync(loan.UserProfileId);
                if (user != null)
                {
                    //#if DEBUG
                    //    user.Email = "adesolaopeyemi216@gmail.com";
                    //#endif
                    if (newStatus == LoanStatus.Approved)
                    {
                        await _emailService.SendLoanApprovalEmailAsync(user, loan);
                    }
                    else if (newStatus == LoanStatus.Rejected) 
                    {
                        await _emailService.SendLoanRejectionEmailAsync(user, loan);
                    }
                }
                return new AdminLoanDto
                {
                    Id = loan.Id,
                    LoanType = loan.LoanType,
                    Amount = loan.Amount,
                    ApprovedAmount = loan.ApprovedAmount,
                    Status = loan.Status,
                    RequestedDate = loan.RequestedDate,
                    ApprovalDate = loan.ApprovalDate,
                    UserProfileId = loan.UserProfileId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<bool> DeleteLoanAsync(string loanId)
        {
            try
            {
                var deleted = await _adminRepository.DeleteLoanAsync(loanId);
                if (!deleted)
                {
                    throw new NotFoundException("Loan not found.");
                }
                return deleted;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // PreQualified Management
        public async Task<PreQualifiedLoanDto?> CreatePreQualifiedLoanAsync(CreatePreQualifiedLoanDto createPqLoan)
        {
            try
            {
                if (createPqLoan.MinAmount < MinLoanAmount)
                    throw new ValidationException($"Minimum loan amount is {MinLoanAmount}");
                if (createPqLoan.MaxAmount > MaxLoanAmount)
                    throw new ValidationException($"Maximum loan amount is {MaxLoanAmount}");
                if (createPqLoan.MaxAmount <= createPqLoan.MinAmount)
                    throw new ValidationException("Maximum amount must be greater than minimum amount");

                var preQualifiedLoan = new PreQualifiedLoan
                {
                    LoanType = createPqLoan.LoanType,
                    MinAmount = createPqLoan.MinAmount,
                    MaxAmount = createPqLoan.MaxAmount,
                    LoanTenure = createPqLoan.LoanTenure
                };

                await _adminRepository.AddPreQualifiedLoanAsync(preQualifiedLoan);

                return new PreQualifiedLoanDto
                {
                    Id = preQualifiedLoan.Id,
                    LoanType = preQualifiedLoan.LoanType,
                    MinAmount = preQualifiedLoan.MinAmount,
                    MaxAmount = preQualifiedLoan.MaxAmount,
                    LoanTenure = preQualifiedLoan.LoanTenure
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<List<PreQualifiedLoanDto>> GetAllPreQualifiedLoansAsync(LoanType? loanType, string? preQualifiedId)
        {
            try
            {
                var allPreQualified = await _adminRepository.GetAllPreQualifiedLoansAsync(loanType, preQualifiedId);
                if (allPreQualified == null)
                {
                    return new List<PreQualifiedLoanDto>();
                }

                return allPreQualified.Select(p => new PreQualifiedLoanDto
                {
                    Id = p.Id,
                    LoanType = p.LoanType,
                    MaxAmount = p.MaxAmount,
                    MinAmount = p.MinAmount,
                    LoanTenure = p.LoanTenure
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<PreQualifiedLoanDto?> GetPreQualifiedLoanByIdAsync(string preQualifiedId)
        {
            try
            {
                var preQualified = await _adminRepository.GetPreQualifiedLoanByIdAsync(preQualifiedId);
                if (preQualified == null)
                    throw new NotFoundException("PreQualified not found");

                return new PreQualifiedLoanDto
                {
                    Id = preQualified.Id,
                    LoanType = preQualified.LoanType,
                    MinAmount = preQualified.MinAmount,
                    MaxAmount = preQualified.MaxAmount,
                    LoanTenure = preQualified.LoanTenure
                };
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }

        public async Task<bool> DeletePreQualifiedLoanAsync(string preQualifiedId)
        {
            try
            {
                var deleted = await _adminRepository.DeletePreQualifiedLoanAsync(preQualifiedId);
                if (!deleted)
                    throw new NotFoundException("PreQualifiedLoan not found");

                return deleted;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }


        // History Management
        public async Task<bool> DeleteLoanHistoryAsync(string loanHistoryId)
        {
            try
            {
                var deleted = await _adminRepository.DeleteLoanHistoryAsync(loanHistoryId);
                if (!deleted)
                    throw new NotFoundException("Loan history not found");

                return deleted;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException(
                    "Service is temporarily unavailable. Please try again later.",
                    ex
                );
            }
        }
    }
}
