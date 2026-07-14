using Infrastructure.DbContexts;
using Application.Services.Interfaces.Repositories;
using Domain.Entities;

namespace Infrastructure.Repositories.Implementations
{
    public class EmailRepository : IEmailRepository
    {
        private readonly AppDbContext _context;

        public EmailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddEmailLogAsync(EmailLog emailLog)
        {
            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();
        }
    }
}
