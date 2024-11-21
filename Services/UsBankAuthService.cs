using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VaultShare.Models;
using System.Text.Json.Serialization;


public class USAuthService
{
    private const string ApiKey = "NJGvfgokPYSPdd3pRny2NG6EB9DGzVAu";
    private const string ApiSecret = "6ASSEr7uMU0adYqG";
    private const string AuthEndpoint = "https://sandbox.usbank.com/auth/oauth2/v1/token";

    public async Task<string> GetAccessTokenAsync()
{
    using var httpClient = new HttpClient();
    Console.WriteLine("[GetAccessTokenAsync] Initializing HttpClient for access token retrieval.");

    // Set Authorization header with Basic auth
    var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiKey}:{ApiSecret}"));
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);
    Console.WriteLine("[GetAccessTokenAsync] Authorization header set.");

    // Set Accept header
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

    // Prepare content as form-urlencoded with the correct Content-Type
    var content = new StringContent("grant_type=client_credentials", Encoding.UTF8);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

    Console.WriteLine("[GetAccessTokenAsync] Content prepared with grant_type=client_credentials and Content-Type set to application/x-www-form-urlencoded.");

    try
    {
        Console.WriteLine("[GetAccessTokenAsync] Sending POST request to AuthEndpoint.");
        var response = await httpClient.PostAsync(AuthEndpoint, content);
        Console.WriteLine($"[GetAccessTokenAsync] Response received with status code: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[GetAccessTokenAsync] Access token retrieval successful.");
            
            var tokenData = JsonSerializer.Deserialize<AccessTokenResponse>(responseData);
            Console.WriteLine("[GetAccessTokenAsync] Access token: " + tokenData.AccessToken);
            return tokenData.AccessToken;
        }
        else
        {
            Console.WriteLine($"[GetAccessTokenAsync] Failed to obtain access token. Status Code: {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GetAccessTokenAsync] Error content: {errorContent}");
            return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GetAccessTokenAsync] Exception during access token retrieval: {ex.Message}");
        return null;
    }
    }
}

public class AccessTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; }

    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; }
}

