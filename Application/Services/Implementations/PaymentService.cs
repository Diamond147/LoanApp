using Application.Exceptions;
using Application.Extensions;
using Application.Services.Interfaces.ExternalServices;
using Application.Services.Interfaces.Repositories;
using Application.Services.Interfaces.Services;
using AutoMapper;
using Domain.DTOs.Payments;
using Domain.Entities;
using Domain.Enums;
using Domain.Helpers;
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
        private readonly IMapper _mapper;

        public PaymentService(ILoanRepository loanRepository, ILoanHistoryRepository loanHistoryRepository, IPaymentRepository paymentRepository, IPaystackClient paystackClient, IEmailService emailService, IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ICacheService cacheService, IMapper mapper)
        {
            _loanRepository = loanRepository;
            _loanHistoryRepository = loanHistoryRepository;
            _paymentRepository = paymentRepository;
            _paystackClient = paystackClient;
            _emailService = emailService;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _cacheService = cacheService;
            _mapper = mapper;
        }

        
        public async Task<PaymentResponseDto> InitiatePaymentAsync()
        {
            try
            {
                var UserInfo = _httpContextAccessor.HttpContext?.User?.GetUserInfo(); // Get all user info at once
                if (UserInfo == null)
                    throw new UnauthorizedAccessException("User is not authenticated");

                var user = await _userRepository.GetUserByIdAsync(UserInfo.UserId);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                // Get user's approved loan
                var loan = await _loanRepository.GetApprovedLoanByUserIdAsync(UserInfo.UserId);
                if (loan == null)
                {
                    throw new NotFoundException("No approved loan found.");
                }

                // Check if loan already paid OR has pending payment
                var existingPayments = await _paymentRepository.GetPaymentsByLoanIdAsync(loan.Id);

                if (existingPayments.Any(p => p.Status == PaymentStatus.Success))
                {
                    throw new ValidationException("This loan has already been paid.");
                }

                var pendingPayment = existingPayments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
                if (pendingPayment != null)
                    return _mapper.Map<PaymentResponseDto>(pendingPayment); // return the existing one instead of creating new


                // This reference is used to Track payment in our database;Verify payment with Paystack;Match webhook notifications to payments
                var paymentReference = $"PAY_{Guid.NewGuid()}";

                // reflects what's actually still owed right now
                var (projectedAccrued, _) = LoanInterestCalculator.CalculateProjectedAccrual(loan, DateTime.UtcNow);
                var amountToPay = loan.PrincipalBalance + projectedAccrued;

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
            //catch (Exception ex)
            //{
            //    // Temporarily log the real error
            //    throw new ExternalServiceUnavailableException(ex.Message, ex); // expose real message
            //}
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
                return false;
            }

            //IDEMPOTENCY CHECK - Prevent duplicate processing
            if (payment.Status == PaymentStatus.Success)
            {
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
                            // Before marking as paid, compute accrued interest up to now and apply payment
                            var (projectedAccrued, projectedDate) = LoanInterestCalculator.CalculateProjectedAccrual(loan, DateTime.UtcNow);
                            loan.AccruedInterest = projectedAccrued;
                            loan.LastInterestAccrualDate = projectedDate ?? loan.LastInterestAccrualDate;

                            // Apply payment amount: interest first, then principal
                            decimal remaining = payment.Amount;
                            if (loan.AccruedInterest > 0)
                            {
                                if (remaining >= loan.AccruedInterest)
                                {
                                    remaining -= loan.AccruedInterest;
                                    loan.AccruedInterest = 0m;
                                }
                                else
                                {
                                    loan.AccruedInterest -= remaining;
                                    remaining = 0m;
                                }
                            }

                            if (remaining > 0 && loan.PrincipalBalance > 0)
                            {
                                loan.PrincipalBalance = Math.Max(0, loan.PrincipalBalance - remaining);
                            }

                            // If outstanding principal fully paid, mark loan as Paid
                            if (loan.PrincipalBalance <= 0)
                            {
                                loan.Status = LoanStatus.Paid;
                            }

                            loan.UpdatedDate = DateTime.UtcNow;

                            await _loanRepository.UpdateLoanAsync(loan);

                            // Invalidate loan caches
                            await _cacheService.RemoveAsync($"loans:id:{loan.Id}");
                            await _cacheService.RemoveAsync($"loans:user:{loan.UserProfileId}");
                            await _cacheService.RemoveByPrefixAsync("loans:all:");

                            // Record loan history for this payment
                            var loanHistory = _mapper.Map<LoanHistory>(loan);

                            await _loanHistoryRepository.AddLoanHistoryAsync(loanHistory);

                            // Send notification email to user
                            if (payment.UserProfileId != null)
                            {
                                var user = await _userRepository.GetUserByIdAsync(payment.UserProfileId);
                                if (user != null)
                                {
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

                    return payments.Select(p => _mapper.Map<PaymentDto>(p)).ToList();
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

                    return _mapper.Map<PaymentDto>(payment);
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

                    return _mapper.Map<PaymentDto>(payment);
                },
                expirationTime: TimeSpan.FromMinutes(10)
            );

            return dto;
        }
    }
}
 