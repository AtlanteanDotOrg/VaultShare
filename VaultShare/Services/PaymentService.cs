using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

    public async Task<bool> PushToCardAsync(PaymentUser sender, PaymentUser recipient, decimal amount)
    {
        try
        {
            var pushToCardPayload = new
            {
                senderCard = new {
                    cardNumber = sender.CardNumber,
                    expiry = sender.CardExpiry,
                    cardCvc = sender.CardCvc
                },
                recipientCard = new {
                    cardNumber = recipient.CardNumber,
                    expiry = recipient.CardExpiry,
                },
                transactionDetails = new {
                    amount = amount,
                    currency = "USD",
                    description = "VaultShare transfer"
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
