// [ApiController]
// [Route("api/paypal")]
// public class PayPalController : ControllerBase
// {
//     private readonly PayPalService _payPalService;

//     public PayPalController()
//     {
//         _payPalService = new PayPalService("YourClientID", "YourClientSecret");
//     }

//     [HttpPost("create-payment")]
//     public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest paymentRequest)
//     {
//         var approvalUrl = await _payPalService.CreatePayment(paymentRequest.Amount);
//         if (approvalUrl != null)
//         {
//             return Ok(new { approvalUrl });
//         }
//         return BadRequest("Unable to create payment");
//     }
// }

// public class PaymentRequest
// {
//     public decimal Amount { get; set; }
//     public string RecipientId { get; set; }
// }
