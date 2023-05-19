using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using YellowDogSoftware.NewDev.Models.AdequateShopApiClient;

namespace YellowDogSoftware.NewDev.Services;

public class AdequateShopApiClientOptions
{
    public string ApiUrl { get; set; } = "http://restapi.adequateshop.com";
}

public class AdequateShopApiClient
{
    private readonly HttpClient _httpClient;

    public AdequateShopApiClient(IOptions<AdequateShopApiClientOptions> options)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.Value.ApiUrl)
        };
    }

    public string? AuthToken { get; set; }

    private string RequireAuthToken()
    {
        var authToken = AuthToken;

        if ( string.IsNullOrWhiteSpace(authToken) )
        {
            throw new InvalidOperationException("Auth token required.");
        }

        return authToken;
    }

    public async Task RegisterAsync(string name, string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/authaccount/registration", new
        {
            name,
            email,
            password
        });

        var result = await response.Content.ReadFromJsonAsync<AdequateShopAuthResponse>()
                     ?? throw new Exception("Failed to deserialize registration response.");

        result.EnsureSuccess();

        AuthToken = result.Data.Token;
    }

    public async Task LogInAsync(string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/authaccount/login", new
        {
            email,
            password
        });

        var result = await response.Content.ReadFromJsonAsync<AdequateShopAuthResponse>()
                     ?? throw new Exception("Failed to deserialize log-in response.");

        result.EnsureSuccess();

        AuthToken = result.Data.Token;
    }

    public async Task<AdequateShopUserPage> GetUserPageAsync(int page)
    {
        var authToken = RequireAuthToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users?page={page}");

        request.Headers.Add("Authorization", $"Bearer {authToken}");

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdequateShopUserPage>() ??
               throw new Exception("Failed to deserialize user page.");
    }
}