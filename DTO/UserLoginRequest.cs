namespace pq_chat_httpserver.DTO;

public class UserLoginRequest
{
    public string EmailAddress {get; set;} = string.Empty;
    public string Password     {get; set;} = string.Empty;
}