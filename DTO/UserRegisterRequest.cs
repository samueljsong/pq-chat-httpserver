namespace pq_chat_httpserver.DTO;

public class UserRegistrationRequest
{
    public string FirstName    {get; set;}
    public string LastName     {get; set;}
    public string EmailAddress {get; set;}
    public string Password     {get; set;}
}