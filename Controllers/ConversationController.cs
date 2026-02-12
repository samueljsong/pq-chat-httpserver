using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pq_chat_httpserver.Services;
using pq_chat_httpserver.DTO;

namespace pq_chat_httpserver.Controllers;

[Authorize]
[ApiController]
[Route("api/conversation")]
public class ConversationController : ControllerBase
{
    private readonly ConversationService _conversationService;
    public ConversationController(ConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost("dm/{recipientUserId}")]
    public async Task<IActionResult> CreateConversation(string recipientUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var conversationId = await _conversationService.CreateOrGetDmAsync(userId, recipientUserId);

        return Ok(new {conversationId});
    }

    [HttpGet("getAll")]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var conversations = await _conversationService.GetAllForUserAsync(userId);

        return Ok(conversations);
    }
}