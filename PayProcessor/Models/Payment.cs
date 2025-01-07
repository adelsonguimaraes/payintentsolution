namespace PayProcessor.Models
{
    public class Payment
    {
        public Guid Uuid { get; set; }
        public double Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
    }
}