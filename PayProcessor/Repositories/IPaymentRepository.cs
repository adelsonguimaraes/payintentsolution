using PayProcessor.Models;

namespace PayProcessor.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> GetPaymentByIdAsync(Guid id);
        Task CreatePaymentAsync(Payment payment);

    }
}