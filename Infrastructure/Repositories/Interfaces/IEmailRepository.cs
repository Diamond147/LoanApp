
using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces
{
    public interface IEmailRepository
    {
        Task AddEmailLogAsync(EmailLog emailLog);
    }
}
