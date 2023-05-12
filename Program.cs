using RestSharp;
using Newtonsoft.Json;
using System.Data.SQLite;
namespace NewDev;

public class AuthResponse 
{
    public string Id { get; set; }
    public int Code { get; set; }
    public string Message { get; set; }
    public AuthResponseData? Data { get; set; }
}

public class AuthResponseData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
}

public class UsersResponse
{
    public string Id { get; set; }
    public int Page { get; set; }
    public int Per_Page { get; set; }
    public int Totalrecord { get; set; }
    public int Total_Pages { get; set; }
    public List<User> Data { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string ProfilePicture { get; set; }
    public string Location { get; set; }
    public string Createdat { get; set; }
}

public class Program 
{
    private static string? AuthToken { get; set; }

    public static void Main(string[] args) 
    {
        // if user table exists, ignore it
        CreateTable();
        
        // Api Url = http://restapi.adequateshop.com
        var url = "http://restapi.adequateshop.com/api";
        RestClient client = new(url);

        // 1: Allow the user to input an email and password (command-line entry)
        Console.WriteLine("Register or Login?: (Input R or L)");
        var loginRegister = Console.ReadLine();

        Console.WriteLine("\n\nEnter Email: ");
        var email = Console.ReadLine();

        Console.WriteLine("Enter Password: ");
        var password = Console.ReadLine();
        
        RestResponse response;

        // 2: Register || Login user into the api
        if (loginRegister == "R") {
            Console.WriteLine("Please enter a name: ");
            var name = Console.ReadLine();
            response = Register(client, email, password, name);
            if (response.Content != null)
            {
                AuthResponse? registrationResponse = JsonConvert.DeserializeObject<AuthResponse>(response.Content);
                if (registrationResponse is not null && registrationResponse.Message == "success") 
                {
                    response = Login(client, email, password);
                }
            }
        } 
        else if (loginRegister == "L")
        {
            response = Login(client, email, password);            
        } else {
            Console.WriteLine("Please Enter Only R for Register or L for Login");
            return;
        }

        if (response.Content is null)
            return;

        AuthResponse? authResp = JsonConvert.DeserializeObject<AuthResponse>(response.Content);

        if (authResp is null || authResp.Data is null)
        {
            if (authResp is null) {
                Console.WriteLine("No response from server");
            } else {
                Console.WriteLine(authResp.Message);
            }
            
            return;
        } else {
            Console.WriteLine();
        }
        
        Console.WriteLine(authResp.Message);
        
        // 3: Save the token for future calls
        AuthToken = authResp.Data.Token;

        // 4: Save the user information into the sqlite database
        CrawlUsers(client);

        // 6: Export to csv
        ExportUsers();
    }

    
    public static RestResponse Login(RestClient client, string email, string password) 
    {
        
        RestRequest request = new("/authaccount/login", Method.Post);
        
        Dictionary<string, string> payload = new()
        {
            {"email", email},
            {"password", password}
        };
        var jsonReq = JsonConvert.SerializeObject(payload);
        
        request.AddJsonBody(jsonReq);
        
        RestResponse response = client.Execute(request);
        return response;
    }

    public static RestResponse Register(RestClient client, string email, string password, string name)
    {
        RestRequest request = new("/authaccount/registration", Method.Post);
        
        Dictionary<string, string> payload = new()
        {
            {"email", email},
            {"password", password},
            {"name", name}
        };
        var jsonReq = JsonConvert.SerializeObject(payload);
        
        request.AddJsonBody(jsonReq);
        
        RestResponse response = client.Execute(request);
        return response;
    }
    
    
    public static void CreateTable() 
    {        
        using SQLiteConnection connection = new($"Data Source=users.db;Version=3;");
        connection.Open();   
        
        using SQLiteCommand command = new("CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, email TEXT, createdAt DATE)", connection);
        command.ExecuteNonQuery();
        
        connection.Close();

    }

    // name, email, createdAt
    public static void InsertInto(User? user) 
    {
        if (user == null) {
            Console.WriteLine("Failed to retrive user data");
            return;
        }
        
        using SQLiteConnection connection = new($"Data Source=users.db;Version=3;");
        connection.Open();   
        
        using SQLiteCommand command = new("INSERT INTO Users (name, email, createdAt) VALUES (@Name, @Email, DATE())", connection);
        command.Parameters.AddWithValue("@Name", user.Name);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.ExecuteNonQuery();
        
        connection.Close();
    }

    public static void ExportUsers() 
    {
        using SQLiteConnection connection = new($"Data Source=users.db;Version=3;");
        connection.Open();   
        
        using SQLiteCommand command = new("SELECT * FROM Users", connection);
        var reader = command.ExecuteReader();
        using StreamWriter outputFile = new StreamWriter("./export.csv");
        while (reader.Read()) {
            var csvLine = $"{reader["id"]},{reader["name"]},{reader["email"]},{reader["createdAt"]}";
            outputFile.WriteLine(csvLine);
        }
        connection.Close();
    }

    public static void CrawlUsers(RestClient client)
    {
        int page = 1;
        
        int totalPages = 10;
        // 5: Call the /users endpoint, paginate through responses and save these records to the sqlite table
        while (page <= totalPages) 
        {
            try
            {
                RestRequest request = new($"/users?page={page}", Method.Get);
                request.AddHeader("Authorization", $"Bearer {AuthToken}");

                RestResponse response = client.Execute(request);

                UsersResponse? uResp = JsonConvert.DeserializeObject<UsersResponse>(response.Content);
                if (uResp is null)
                    return;

                List<User> users = uResp.Data;

                // Opted out of using LINQ for that minor performance gain 🔥
                foreach(var user in users)
                {
                    InsertInto(user);
                }

            } finally {
                page++;
            }
        }
    }
}