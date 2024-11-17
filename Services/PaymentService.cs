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
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://sandbox.usbank.com") // Updated base address
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        Console.WriteLine("PaymentService initialized with base address: https://sandbox.usbank.com");
        Console.WriteLine($"Bearer token set: {_accessToken.Substring(0, 10)}..."); // Show only the first part of the token
    }

    public async Task<bool> PushToCardAsync(User sender, User recipient, decimal amount)
    {
        try
        {
            Console.WriteLine("Starting PushToCardAsync process...");

            // Generate unique IDs for correlation and idempotency
            var correlationId = Guid.NewGuid().ToString();
            var idempotencyKey = Guid.NewGuid().ToString();
            Console.WriteLine($"Generated Correlation-ID: {correlationId}");
            Console.WriteLine($"Generated Idempotency-Key: {idempotencyKey}");

            // Set additional headers explicitly
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Correlation-ID", correlationId);
            _httpClient.DefaultRequestHeaders.Add("idempotency-key", idempotencyKey);
            Console.WriteLine("Headers set: Accept, Accept-Encoding, Correlation-ID, Idempotency-Key");

            // Log sender and recipient details for verification
            Console.WriteLine($"Sender Details: Name={sender.Name}, AccountNumber={sender.AccountNumber}, RoutingNumber={sender.RoutingNumber}, MerchantID={sender.MerchantID}");
            Console.WriteLine($"Recipient Details: Name={recipient.Name}, CardNumber={recipient.CardNumber}, ExpirationDate={recipient.CardExpiry}, ZipCode=60606");
            Console.WriteLine($"Transfer Amount: {amount}");

            // Build payload for push-to-card transfer
            var pushToCardPayload = new
            {
                transfer = new
                {
                    clientDetails = new
                    {
                        clientRequestID = correlationId
                    },
                    senderDetails = new
                    {
                        name = sender.Name,
                        accountNumber = sender.AccountNumber,
                        routingNumber = sender.RoutingNumber,
                        merchantID = sender.MerchantID,
                        address = new
                        {
                            addressLine1 = "100 Main St",
                            addressLine2 = "Apt 116",
                            city = "Chicago",
                            state = "IL",
                            zipCode = "60606"
                        }
                    },
                    recipientDetails = new
                    {
                        name = recipient.Name,
                        cardNumber = recipient.CardNumber,
                        expirationDate = recipient.CardExpiry,
                        zipCode = "60606"
                    },
                    amount = amount.ToString("F2"), // Format amount as a string with two decimal places
                    paymentpurpose = "Debt amount",
                    paymentMemo = "VaultShare transfer"
                }
            };

            // Serialize the payload to JSON
            var jsonPayload = JsonSerializer.Serialize(pushToCardPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Log payload for debugging
            Console.WriteLine("Payload sent to API:");
            Console.WriteLine(jsonPayload);

            // Send POST request to the correct endpoint path
            Console.WriteLine("Sending POST request to /push-to-card/v1/transfers...");
            var response = await _httpClient.PostAsync("/push-to-card/v1/transfers", content);

            // Log response status and content
            Console.WriteLine($"Response Status Code: {response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Payment successfully sent!");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Failed to process payment.");
                Console.WriteLine("Error response content:");
                Console.WriteLine(errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during payment: {ex.Message}");
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }

    public async Task<bool> PushToCardAsync(User sender, Vault recipient, decimal amount)
{
    try
    {
        Console.WriteLine("Starting PushToCardAsync process...");

        // Generate unique IDs for correlation and idempotency
        var correlationId = Guid.NewGuid().ToString();
        var idempotencyKey = Guid.NewGuid().ToString();
        Console.WriteLine($"Generated Correlation-ID: {correlationId}");
        Console.WriteLine($"Generated Idempotency-Key: {idempotencyKey}");

        // Set additional headers explicitly
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Correlation-ID", correlationId);
        _httpClient.DefaultRequestHeaders.Add("idempotency-key", idempotencyKey);
        Console.WriteLine("Headers set: Accept, Accept-Encoding, Correlation-ID, Idempotency-Key");

        // Log sender and recipient details for verification
        Console.WriteLine($"Sender Details: Name={sender.Name}, AccountNumber={sender.AccountNumber}, RoutingNumber={sender.RoutingNumber}, MerchantID={sender.MerchantID}");
        Console.WriteLine($"Recipient Details: Name={recipient.Name}, CardNumber={recipient.CardNumber}, ExpirationDate={recipient.CardExpiry}, ZipCode=60606");
        Console.WriteLine($"Transfer Amount: {amount}");

        // Format recipient's expiration date
        var formattedExpiryDate = recipient.CardExpiry.Contains("/") 
            ? recipient.CardExpiry.Replace("/", "") // Convert MM/YY to MMYY
            : recipient.CardExpiry;

        if (formattedExpiryDate.Length != 4 || !int.TryParse(formattedExpiryDate, out _))
        {
            Console.WriteLine($"[PushToCardAsync] Invalid expiration date format: {formattedExpiryDate}");
            throw new ArgumentException("Invalid expiration date format. Expected MMYY.");
        }

        Console.WriteLine($"Formatted Expiration Date: {formattedExpiryDate}");

        // Build payload for push-to-card transfer
        var pushToCardPayload = new
        {
            transfer = new
            {
                clientDetails = new
                {
                    clientRequestID = correlationId
                },
                senderDetails = new
                {
                    name = sender.Name,
                    accountNumber = sender.AccountNumber,
                    routingNumber = sender.RoutingNumber,
                    merchantID = sender.MerchantID,
                    address = new
                    {
                        addressLine1 = "100 Main St",
                        addressLine2 = "Apt 116",
                        city = "Chicago",
                        state = "IL",
                        zipCode = "60606"
                    }
                },
                recipientDetails = new
                {
                    name = recipient.Name,
                    cardNumber = recipient.CardNumber,
                    expirationDate = formattedExpiryDate, // Use formatted expiration date
                    zipCode = "60606"
                },
                amount = amount.ToString("F2"), // Format amount as a string with two decimal places
                paymentpurpose = "Debt amount",
                paymentMemo = "VaultShare transfer"
            }
        };

        // Serialize the payload to JSON
        var jsonPayload = JsonSerializer.Serialize(pushToCardPayload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Log payload for debugging
        Console.WriteLine("Payload sent to API:");
        Console.WriteLine(jsonPayload);

        // Send POST request to the correct endpoint path
        Console.WriteLine("Sending POST request to /push-to-card/v1/transfers...");
        var response = await _httpClient.PostAsync("/push-to-card/v1/transfers", content);

        // Log response status and content
        Console.WriteLine($"Response Status Code: {response.StatusCode}");
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Payment successfully sent!");
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Failed to process payment.");
            Console.WriteLine("Error response content:");
            Console.WriteLine(errorContent);
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception during payment: {ex.Message}");
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);
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
