using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VaultShare.Models;


public class USAuthService
{
    private const string ApiKey = "qOztROJZpvqHBCjuPDAA5hl8lPuTMWGq";
    private const string ApiSecret = "oly4Y2DXGsWJgjaP";
    private const string AuthEndpoint = "https://sandbox.api.usbank.com/oauth/token"; // Authentication endpoint

    public async Task<string> GetAccessTokenAsync()
    {
        using var httpClient = new HttpClient();

        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiKey}:{ApiSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.PostAsync(AuthEndpoint, content);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<AccessTokenResponse>(responseData);
            return tokenData.AccessToken;
        }
        else
        {
            Console.WriteLine($"Failed to obtain access token. Status Code: {response.StatusCode}");
            return null;
        }
    }
}

public class AccessTokenResponse
{
    public string AccessToken { get; set; }
}
