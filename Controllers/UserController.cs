using Microsoft.AspNetCore.Mvc;
using pq_chat_httpserver.Services;
using pq_chat_httpserver.DTO;

namespace pq_chat_httpserver.Controllers;

[ApiController] 
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("createUser")]
    public async Task<IActionResult> CreateUser([FromBody] UserRegistrationRequest request)
    {
        Console.WriteLine(request);

        await _userService.CreateUser
        (
            request.FirstName, 
            request.LastName, 
            request.EmailAddress, 
            request.Password
        );

        return Ok();
    }

    [HttpPost("loginUser")]
    public async Task<IActionResult> LoginUser([FromBody] UserLoginRequest request)
    {
        var token = await _userService.LoginUser
        (
            request.EmailAddress,
            request.Password
        );

        if (token == null)
            return Unauthorized("Invalid email or password");

        return Ok(new { token });
    }
}