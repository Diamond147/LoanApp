using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Payments;
using Domain.Entities;
using Domain.Enums;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace Application.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaystackClient _paystackClient;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(ILoanRepository loanRepository, IPaymentRepository paymentRepository, IPaystackClient paystackClient, IEmailService emailService, IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _loanRepository = loanRepository;
            _paymentRepository = paymentRepository;
            _paystackClient = paystackClient;
            _emailService = emailService;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        //public async Task<PaymentResponseDto> InitiatePaymentAsync(InitiatePaymentDto initiatePayment)
        public async Task<PaymentResponseDto> InitiatePaymentAsync()
        {
            try
            {
                var UserInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo(); // Get all user info at once
                if (UserInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                // Get user's approved loan
                var loan = await _loanRepository.GetApprovedLoanByUserIdAsync(UserInfo.UserId);
                if (loan == null)
                {
                    throw new NotFoundException("No approved loan found.");
                }

                // check if loan already paid
                var existingPayments = await _paymentRepository.GetPaymentsByLoanIdAsync(loan.Id);
                if (existingPayments.Any(p => p.Status == PaymentStatus.Success))
                {
                    throw new ValidationException("This loan has already been paid.");
                }

                var user = await _userRepository.GetUserByIdAsync(UserInfo.UserId);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                // This reference is used to Track payment in our database,Verify payment with Paystack,Match webhook notifications to payments
                var paymentReference = $"PAY_{Guid.NewGuid()}";

                //Get payment amount when Admin approves the loan
                var amountToPay = loan.ApprovedAmount ?? loan.Amount;

                //Create payment record to database
                var payment = new Payment
                {
                    Id = Guid.NewGuid().ToString(),
                    LoanId = loan.Id,
                    UserProfileId = UserInfo.UserId,
                    PaystackReference = paymentReference,
                    Amount = amountToPay,
                    Status = PaymentStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };
                await _paymentRepository.CreatePaymentAsync(payment);

                // Initialize transaction with Paystack and this creates a checkout session on Paystack's servers
                var paystackResponse = await _paystackClient.InitializeTransactionAsync(
                    email: user.Email,
                    amount: amountToPay,
                    reference: paymentReference
                );

                //Extract important fields from the Paystack response (JSON object with nested data structure)
                var responseData = paystackResponse.GetProperty("data");
                var authorizationUrl = responseData.GetProperty("authorization_url").GetString();

                // Update payment record with Paystack response details
                payment.AuthorizationUrl = authorizationUrl;
                payment.PaystackResponse = paystackResponse.ToString();
                payment.UpdatedDate = DateTime.UtcNow;

                await _paymentRepository.UpdatePaymentAsync(payment);

                return new PaymentResponseDto
                {
                    AuthorizationUrl = authorizationUrl ?? string.Empty,
                    Reference = paymentReference,
                    Amount = amountToPay,
                    LoanId = loan.Id
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExternalServiceUnavailableException("Service is temporarily unavailable. Please try again later.", ex);
            }
        }


        // This method is called by the webhook when Paystack confirms a payment.It handles the entire post-payment workflow.
        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            var payment = await _paymentRepository.GetPaymentByReferenceAsync(reference);
            if (payment == null)
            {
                Console.WriteLine($"Payment not found for reference {reference}");
                return false;
            }

            //IDEMPOTENCY CHECK - Prevent duplicate processing
            if (payment.Status == PaymentStatus.Success)
            {
                Console.WriteLine("DEBUG: Payment already Success. Exiting to avoid duplicate email.");
                return true;
            }

            try
            {
                // Verify payment with Paystack API
                var verificationResponse = await _paystackClient.VerifyTransactionAsync(reference);
                using var doc = JsonDocument.Parse(verificationResponse.ToString());
                var data = doc.RootElement.GetProperty("data");
                var status = data.GetProperty("status").GetString();
                var paidAmount = data.GetProperty("amount").GetInt64() / 100m;
                var verifiedReference = data.GetProperty("reference").GetString();

                //All CHECKS must pass for payment to be accepted
                if (status == "success" && paidAmount == payment.Amount && verifiedReference == reference)
                {
                    //Update payment status to Success
                    payment.Status = PaymentStatus.Success;
                    payment.PaystackResponse = verificationResponse.ToString(); //Store full response for audit
                    payment.UpdatedDate = DateTime.UtcNow;
                    await _paymentRepository.UpdatePaymentAsync(payment);
                    Console.WriteLine("DEBUG: Payment record updated in DB.");

                    //Update loan status to Paid
                    if (payment.LoanId != null)
                    {
                        var loan = await _loanRepository.GetLoanByIdAsync(payment.LoanId);

                        if (loan != null && loan.Status != LoanStatus.Paid)
                        {
                            loan.Status = LoanStatus.Paid;
                            loan.UpdatedDate = DateTime.UtcNow;
                            await _loanRepository.UpdateLoanAsync(loan);
                            Console.WriteLine("DEBUG: Loan record updated to Paid.");

                            // Check if history already exists for this loan payment
                            var historyExists = await _loanRepository.historyExists(loan.Id);

                            if (!historyExists) // Only create if it doesn't exist
                            {
                                var loanHistory = new LoanHistory
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    LoanId = loan.Id,
                                    LoanType = loan.LoanType,
                                    RequestedAmount = loan.Amount,
                                    ApprovedAmount = loan.ApprovedAmount,
                                    RequestedDate = loan.RequestedDate,
                                    ApprovalDate = loan.ApprovalDate,
                                    Status = LoanStatus.Paid,
                                    UserProfileId = loan.UserProfileId,
                                };

                                // Save updates to db
                                await _loanRepository.AddLoanHistoryAsync(loanHistory);
                                Console.WriteLine("DEBUG: History recorded.");
                            }

                            // Send notification email to user
                            if (payment.UserProfileId != null)
                            {
                                var user = await _userRepository.GetUserByIdAsync(payment.UserProfileId);
                                if (user != null)
                                {
                                    Console.WriteLine($"DEBUG: Attempting to send email to {user.Email}...");
                                    await _emailService.SendPaymentConfirmationEmailAsync(user, loan, payment);
                                }
                                else
                                {
                                    Console.WriteLine("Email not sent: User not found."); 
                                }
                            }
                        }
                    }

                    return true;
                }

                // Payment verification failed
                Console.WriteLine($"Payment verification failed for {reference}:");
                Console.WriteLine($"  Expected status: success, Got: {status}");
                Console.WriteLine($"  Expected amount: {payment.Amount}, Got: {paidAmount}");
                Console.WriteLine($"  Expected reference: {reference}, Got: {verifiedReference}");

                //Update payment status to failed
                payment.Status = PaymentStatus.Failed;
                payment.PaystackResponse = verificationResponse.ToString();
                payment.UpdatedDate = DateTime.UtcNow;
                await _paymentRepository.UpdatePaymentAsync(payment);

                //Get user and loan for failure notification
                if (!string.IsNullOrEmpty(payment.UserProfileId) && !string.IsNullOrEmpty(payment.LoanId))
                {
                    var userFail = await _userRepository.GetUserByIdAsync(payment.UserProfileId);

                    var loanFail = await _loanRepository.GetLoanByIdAsync(payment.LoanId);

                    //send failure email to user
                    if (userFail != null && loanFail != null)
                    {
                        await _emailService.SendPaymentFailureEmailAsync(userFail, loanFail, payment);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying payment {reference}: {ex.Message}");

                // Update payment status to Failed
                payment.Status = PaymentStatus.Failed;
                payment.UpdatedDate = DateTime.UtcNow;
                await _paymentRepository.UpdatePaymentAsync(payment);

                return false;
            }
        }


        public async Task<List<PaymentDto>> GetPaymentsAsync(PaymentStatus? status, string? paymentId, string? reference)
        {
            var payments = await _paymentRepository.GetPaymentsAsync(status, paymentId, reference);

            // Map to DTOs manually
            return payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                LoanId = p.LoanId,
                UserProfileId = p.UserProfileId,
                Amount = p.Amount,
                Status = p.Status,
                CreatedDate = p.CreatedDate,
            }).ToList();
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(string paymentId)
        {
            var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
            if (payment == null)
                throw new NotFoundException("Payment not found.");
            return new PaymentDto
            {
                Id = payment.Id,
                LoanId = payment.LoanId,
                UserProfileId = payment.UserProfileId,
                Amount = payment.Amount,
                Status = payment.Status,
                CreatedDate = payment.CreatedDate,
            };
        }

        public async Task<PaymentDto?> GetPaymentByReferenceAsync(string reference)
        {
            var payment = await _paymentRepository.GetPaymentByReferenceAsync(reference);
            if (payment == null)
                throw new NotFoundException("Payment not found.");
            return new PaymentDto
            {
                Id = payment.Id,
                LoanId = payment.LoanId,
                UserProfileId = payment.UserProfileId,
                Amount = payment.Amount,
                Status = payment.Status,
                CreatedDate = payment.CreatedDate,
            };
        }
    }
}
 