using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.Services;
using Domain.DTOs.Payments;
using Domain.Entities;
using Domain.Enums;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using System.Text.Json;


namespace Application.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ILoanHistoryRepository _loanHistoryRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaystackClient _paystackClient;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheService _cacheService;

        public PaymentService(ILoanRepository loanRepository, ILoanHistoryRepository loanHistoryRepository, IPaymentRepository paymentRepository, IPaystackClient paystackClient, IEmailService emailService, IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ICacheService cacheService)
        {
            _loanRepository = loanRepository;
            _loanHistoryRepository = loanHistoryRepository;
            _paymentRepository = paymentRepository;
            _paystackClient = paystackClient;
            _emailService = emailService;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _cacheService = cacheService;
        }

        
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

                // This reference is used to Track payment in our database;Verify payment with Paystack;Match webhook notifications to payments
                var paymentReference = $"PAY_{Guid.NewGuid()}";

                //Get payment amount when Admin approves the loan
                var amountToPay = loan.RequestedAmount;

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

                // Extract important fields from the Paystack response (JSON object with nested data structure)
                var responseData = paystackResponse.GetProperty("data");
                var authorizationUrl = responseData.GetProperty("authorization_url").GetString();

                // Update payment record with Paystack response details
                payment.AuthorizationUrl = authorizationUrl;
                payment.PaystackResponse = paystackResponse.ToString();
                payment.UpdatedDate = DateTime.UtcNow;

                await _paymentRepository.UpdatePaymentAsync(payment);

                // Invalidate related caches so fresh data is returned
                await _cacheService.RemoveAsync($"payments:id:{payment.Id}");
                await _cacheService.RemoveAsync($"payments:ref:{payment.PaystackReference}");
                await _cacheService.RemoveByPrefixAsync("payments:");
                //await _cacheService.RemoveAsync($"loans:id:{loan.Id}");
                //await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
                //await _cacheService.RemoveByPrefixAsync("loans:all:");

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
        public async Task<bool> ProcessSuccessfulWebhookAsync(PaystackWebhookData data)
        {
            var reference = data.Reference;

            var payment = await _paymentRepository.GetPaymentByReferenceAsync(reference);
            if (payment == null)
            {
                Console.WriteLine($"Payment not found for reference {reference}");
                return false;
            }

            //IDEMPOTENCY CHECK - Prevent duplicate processing
            if (payment.Status == PaymentStatus.Success)
            {
                Console.WriteLine("DEBUG: Payment already Successful. Exiting to avoid duplicate email.");
                return true;
            }

            decimal AmountInNaira = data.Amount / 100m;

            //All CHECKS must pass for payment to be accepted
            if (data.Status == "success" && AmountInNaira == payment.Amount && data.Reference == reference)
            {
                try
                {

                    //Update payment status to Successful
                    payment.Status = PaymentStatus.Success;
                    payment.PaystackResponse = JsonSerializer.Serialize(data); //Store full response for audit
                    payment.UpdatedDate = DateTime.UtcNow;

                    await _paymentRepository.UpdatePaymentAsync(payment);
                    Console.WriteLine("DEBUG: Payment record updated in DB.");

                    // Invalidate payment caches
                    await _cacheService.RemoveAsync($"payments:id:{payment.Id}");
                    if (!string.IsNullOrEmpty(payment.PaystackReference))
                        await _cacheService.RemoveAsync($"payments:ref:{payment.PaystackReference}");
                    await _cacheService.RemoveByPrefixAsync("payments:");

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

                            // Invalidate loan caches
                            await _cacheService.RemoveAsync($"loans:id:{loan.Id}");
                            await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
                            await _cacheService.RemoveByPrefixAsync("loans:all:");

                            // Check if history already exists for this loan payment
                            var historyExists = await _loanHistoryRepository.historyExists(loan.Id);

                            if (!historyExists) // Only create if it doesn't exist
                            {
                                var loanHistory = new LoanHistory
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    LoanId = loan.Id,
                                    LoanType = loan.LoanType,
                                    RequestedAmount = loan.RequestedAmount,
                                    //ApprovedAmount = loan.ApprovedAmount,
                                    RequestedDate = loan.RequestedDate,
                                    UpdatedDate = loan.UpdatedDate,
                                    Status = LoanStatus.Paid,
                                    UserProfileId = loan.UserProfileId,
                                };

                                // Save updates to db
                                await _loanHistoryRepository.AddLoanHistoryAsync(loanHistory);
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error verifying payment {reference}: {ex.Message}");
                    return false;
                }
            }

            // Fraud / Validation mismatch
            Console.WriteLine($"Payment verification failed for {reference}: Amount or status mismatch.");
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedDate = DateTime.UtcNow;

            await _paymentRepository.UpdatePaymentAsync(payment);

            return false;
        }


        public async Task<List<PaymentDto>> GetPaymentsAsync(PaymentStatus? status, string? paymentId, string? reference)
        {
            string cacheKey = $"payments:filter:status={(status?.ToString()??"all")}:id={paymentId ?? "none"}:ref={reference ?? "none"}";

            var list = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
                {
                    var payments = await _paymentRepository.GetPaymentsAsync(status, paymentId, reference);
                    return payments.Select(p => new PaymentDto
                    {
                        Id = p.Id,
                        LoanId = p.LoanId,
                        UserProfileId = p.UserProfileId,
                        Amount = p.Amount,
                        Status = p.Status,
                        CreatedDate = p.CreatedDate,
                    }).ToList();
                },
                expirationTime: TimeSpan.FromMinutes(10)
            );

            return list ?? new List<PaymentDto>();
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(string paymentId)
        {
            string cacheKey = $"payments:id:{paymentId}";

            var dto = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
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
                },
                expirationTime: TimeSpan.FromMinutes(10)
            );

            return dto;
        }

        public async Task<PaymentDto?> GetPaymentByReferenceAsync(string reference)
        {
            string cacheKey = $"payments:ref:{reference}";

            var dto = await _cacheService.GetOrSetAsync(
                key: cacheKey,
                getItemCallback: async () =>
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
                },
                expirationTime: TimeSpan.FromMinutes(10)
            );

            return dto;
        }
    }
}
 