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

    [HttpGet]
    public IActionResult GetAllUsers()
    {
        var result = _userService.GetAllUsers();

        return Ok(result);
    }

    [HttpPost("createUser")]
    public async Task<IActionResult> CreateUser([FromBody] UserRegistrationRequest request)
    {
        await _userService.CreateUser
        (
            request.FirstName, 
            request.LastName, 
            request.EmailAddress, 
            request.Password
        );

        return Ok();
    }
}