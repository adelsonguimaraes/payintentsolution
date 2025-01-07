using System.ComponentModel.DataAnnotations;

namespace PayIntentAPI.Models
{
    public class PaymentModel
    {

        public PaymentModel()
        {
            Uuid = Guid.NewGuid();
            Status = "Pending";
        }

        public Guid Uuid { get; private set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public double Amount { get; set; }

        [Required(ErrorMessage = "Currency is required")]
        public string? Currency { get; set; }
        public string Status { get; private set; }
    }
}