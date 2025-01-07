using Microsoft.AspNetCore.Mvc;
using PayProcessor.DTOs;
using PayProcessor.Repositories;
using System;
using System.Threading.Tasks;

namespace PayProcessor
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayProcessorController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;

        public PayProcessorController(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        [HttpGet("{uuid:guid}")]
        public async Task<IActionResult> GetPaymentByUuid(Guid uuid)
        {
            Console.WriteLine($"GetPaymentByUuid: {uuid}");

            var payment = await _paymentRepository.GetPaymentByIdAsync(uuid);
            if (payment is null)
            {
                return NotFound("Payment not found");
            }

            return Ok(payment);
        }
    }
}
