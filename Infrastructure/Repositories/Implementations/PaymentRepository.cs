using Infrastructure.DbContexts;
using Application.Services.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Infrastructure.Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;
        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await  _context.SaveChangesAsync();
            return payment;
        }

        public async Task<List<Payment>> GetPaymentsAsync(PaymentStatus? status, string? paymentId, string? reference)
        {
            var query = _context.Payments.AsQueryable();
            if (!string.IsNullOrEmpty(paymentId))
            {
                query = query.Where(py => py.Id == paymentId);
            }

            if (status.HasValue)
            {
                query = query.Where(py => py.Status == status);
            }
            if (!string.IsNullOrEmpty(reference))
            {
                query = query.Where(py => py.PaystackReference == reference);
            }

            query = query.OrderByDescending(py => py.CreatedDate);

            return await query.ToListAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(string paymentId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<List<Payment>> GetPaymentsByLoanIdAsync(string loanId)
        {
            return await _context.Payments
                .Where(p => p.LoanId == loanId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByReferenceAsync(string reference)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.PaystackReference == reference);
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
    }
}
