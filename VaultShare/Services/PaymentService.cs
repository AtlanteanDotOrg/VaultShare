using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VaultShare.Models;

public class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public PaymentService(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://sandbox.api.usbank.com");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    public async Task<bool> PushToCardAsync(User sender, User recipient, decimal amount)
{
    try
    {
        var pushToCardPayload = new
        {
            transfer = new
            {
                clientDetails = new
                {
                    clientRequestID = Guid.NewGuid().ToString()
                },
                senderDetails = new
                {
                    name = sender.Name,
                    accountNumber = sender.AccountNumber,
                    routingNumber = sender.RoutingNumber,
                    merchantID = sender.MerchantID,
                    address = new
                    {
                        addressLine1 = "Sender Address Line 1",  // You may want to replace these with actual data
                        addressLine2 = "Sender Address Line 2",
                        city = "Sender City",
                        state = "Sender State",
                        zipCode = "Sender Zip Code"
                    }
                },
                recipientDetails = new
                {
                    name = recipient.Name,
                    cardNumber = recipient.CardNumber,
                    expirationDate = recipient.CardExpiry,
                    zipCode = "Recipient Zip Code"  // Add additional fields as necessary
                },
                amount = amount,
                paymentpurpose = "Transfer",
                paymentMemo = "VaultShare transfer"
            }
        };

        var jsonPayload = JsonSerializer.Serialize(pushToCardPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/instant-payments/v1/push-to-card", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Payment successfully sent!");
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to process payment: {errorContent}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception during payment: {ex.Message}");
        return false;
    }
}

}

public class PaymentUser
{
    public string CardNumber { get; set; }
    public string CardExpiry { get; set; }
    public string CardCvc { get; set; }
    public string GoogleId { get; internal set; }
}
