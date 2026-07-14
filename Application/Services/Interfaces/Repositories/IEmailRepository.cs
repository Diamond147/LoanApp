using Domain.Entities;

namespace Application.Services.Interfaces.Repositories
{
    public interface IEmailRepository
    {
        Task AddEmailLogAsync(EmailLog emailLog);
    }
}
