using System.Text.Json.Serialization;

namespace YellowDogSoftware.NewDev.Models.AdequateShopApiClient;

public class AdequateShopUser
{
    public AdequateShopUser(int id, string name, string email)
    {
        Id = id;

        Name = name;

        Email = email;
    }

    [JsonPropertyName("id")]
    public int Id { get; private set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("propertyname")]
    public string? ProfilePicture { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("createdat")]
    public DateTimeOffset? CreatedAt { get; set; }
}

public class AdequateShopUserPage
{
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("totalrecord")]
    public int TotalRecords { get; init; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; init; }

    [JsonPropertyName("data")]
    public List<AdequateShopUser> Data { get; init; } = null!;
}