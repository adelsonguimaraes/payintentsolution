using PayProcessor.Models;

namespace PayProcessor.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> GetPaymentByIdAsync(Guid uuid);
        Task CreatePaymentAsync(Payment payment);

    }
}