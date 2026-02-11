using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using pq_chat_httpserver.DTO;
using pq_chat_httpserver.Services;

namespace pq_chat_httpserver.Controllers;

[Authorize]
[ApiController]
[Route("api/friendship")]
public class FriendshipController : ControllerBase
{
    private readonly FriendshipService _friendshipService;

    public FriendshipController(FriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    [HttpPost("sendRequest")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendshipSendRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            await _friendshipService.CreateFriendRequest
            (
                request.RecipientUserId,
                userId
            );

            return Ok();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return StatusCode(500, "Something went wrong");
        }
    }

    [HttpGet("getIncomingRequest")]
    public async Task<IActionResult> GetIncomingFriendRequests()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var incomingFriendRequestList = await _friendshipService.GetIncomingFriendRequests(userId);

        return Ok(incomingFriendRequestList);
    }

    [HttpGet("getAllFriends")]
    public async Task<IActionResult> GetAllFriends()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();
        
        var currentFriendList = await _friendshipService.GetAllFriends(userId);

        return Ok(currentFriendList);
    }

    [HttpGet("getPendingRequests")]
    public async Task<IActionResult> GetPendingFrindRequests()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var pendingFriendRequestList = await _friendshipService.GetPendingFriendRequests(userId);

        return Ok(pendingFriendRequestList);
    }
}