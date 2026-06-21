using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaLikeController : ControllerBase
    {
        private readonly IMediaLikeService _mediaLikeService;

        public MediaLikeController(IMediaLikeService mediaLikeService)
        {
            _mediaLikeService = mediaLikeService;
        }

        /// <summary>Beğeni ekle/kaldır. Yanıt: { isLiked: bool }</summary>
        [HttpPost("{mediaItemId}")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(Guid mediaItemId, [FromQuery] string type)
        {
            if (type != "image" && type != "video")
                return BadRequest(new { message = "type 'image' veya 'video' olmalı." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var isLiked = await _mediaLikeService.ToggleLikeAsync(userId, mediaItemId, type);
            return Ok(new { isLiked });
        }

        /// <summary>Kullanıcının beğendiği medya öğelerini döner (Favoriler sayfası).</summary>
        [HttpGet("my-likes")]
        [Authorize]
        public async Task<IActionResult> GetMyLikes()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var items = await _mediaLikeService.GetLikedByUserAsync(userId);
            return Ok(items);
        }
    }
}
