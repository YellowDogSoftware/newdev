using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace YellowDogSoftware.NewDev.Models.AdequateShopApiClient;

public class AdequateShopAuthResponseData
{
    public AdequateShopAuthResponseData(int id, string name, string email, string token)
    {
        Id = id;

        Name = name;

        Email = email;

        Token = token;
    }

    public int Id { get; init; }

    public string Name { get; init; }

    public string Email { get; init; }

    public string Token { get; init; }
}

public class AdequateShopAuthResponse
{
    public AdequateShopAuthResponse(string id, int code, string message, AdequateShopAuthResponseData? data)
    {
        Id = id;

        Code = code;

        Message = message;

        Data = data;
    }

    [JsonPropertyName("$id")]
    public string Id { get; init; }

    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; }

    [JsonPropertyName("data")]
    public AdequateShopAuthResponseData? Data { get; init; }

    [MemberNotNull(nameof(Data))]
    public void EnsureSuccess()
    {
        if ( Code != 0 )
        {
            throw new Exception($"Code: {Code}; Message: {Message}");
        }

        else if ( Data == null )
        {
            throw new Exception("Invalid auth data.");
        }
    }
}