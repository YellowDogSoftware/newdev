namespace YellowDogSoftware.NewDev.Models;

public class User
{
    public User(int id, string? name, string email)
    {
        Id = id;

        Name = name;

        Email = email;
    }

    public int Id { get; private set; }

    public string? Name { get; set; }

    public string Email { get; set; }

    public string? ProfilePicture { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
}