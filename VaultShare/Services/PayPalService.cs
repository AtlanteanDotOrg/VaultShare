// using PayPalCheckoutSdk.Orders;
// using PayPal.Core;
// using System.Threading.Tasks;

// public class PayPalService
// {
//     private readonly PayPalHttpClient _client;

//     public PayPalService(string clientId, string clientSecret)
//     {
//         var environment = new SandboxEnvironment(clientId, clientSecret);
//         _client = new PayPalHttpClient(environment);
//     }

//     public async Task<string> CreatePayment(decimal amount)
//     {
//         var orderRequest = new OrderRequest()
//         {
//             CheckoutPaymentIntent = "CAPTURE",
//             PurchaseUnits = new List<PurchaseUnitRequest>
//             {
//                 new PurchaseUnitRequest
//                 {
//                     AmountWithBreakdown = new AmountWithBreakdown
//                     {
//                         CurrencyCode = "USD",
//                         Value = amount.ToString("F2")
//                     }
//                 }
//             }
//         };

//         var request = new OrdersCreateRequest();
//         request.Prefer("return=representation");
//         request.RequestBody(orderRequest);

//         var response = await _client.Execute(request);
//         var result = response.Result<Order>();

//         return result.Links.Find(link => link.Rel == "approve")?.Href;
//     }

//     // Example for sending funds to recipient
// public async Task<bool> SendPayout(decimal amount, string recipientEmail)
// {
//     var request = new PayoutsPostRequest();
//     request.RequestBody(new PayoutBatch()
//     {
//         SenderBatchHeader = new SenderBatchHeader
//         {
//             EmailSubject = "You have a payout!",
//             SenderBatchId = Guid.NewGuid().ToString()
//         },
//         Items = new List<PayoutItem>
//         {
//             new PayoutItem
//             {
//                 RecipientType = "EMAIL",
//                 Amount = new Currency
//                 {
//                     Value = amount.ToString("F2"),
//                     CurrencyCode = "USD"
//                 },
//                 Receiver = recipientEmail
//             }
//         }
//     });

//     var response = await _client.Execute(request);
//     return response.StatusCode == System.Net.HttpStatusCode.Created;
// }

// }

