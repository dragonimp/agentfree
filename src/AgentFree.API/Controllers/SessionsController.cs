using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentFree.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(AppDbContext context, ILogger<SessionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/sessions - 获取所有会话
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            return await _context.Sessions
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();
        }

        // GET /api/sessions/{id} - 获取单个会话
        [HttpGet("{id}")]
        public async Task<ActionResult<Session>> GetSession(string id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();
            return session;
        }

        // GET /api/sessions/{id}/messages - 获取会话消息
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<IEnumerable<Message>>> GetMessages(string id)
        {
            var messages = await _context.Messages
                .Where(m => m.SessionId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return messages;
        }

        // POST /api/sessions - 创建会话
        [HttpPost]
        public async Task<ActionResult<Session>> CreateSession([FromBody] CreateSessionDto dto)
        {
            var session = new Session
            {
                AgentId = dto.AgentId,
                Name = dto.Name
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }

        // PUT /api/sessions/{id} - 更新会话
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(string id, [FromBody] UpdateSessionDto dto)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            if (dto.Name != null)
                session.Name = dto.Name;

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/sessions/{id} - 删除会话
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(string id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST /api/sessions/{id}/messages - 发送消息
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<Message>> SendMessage(string id, [FromBody] CreateMessageDto dto)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            var message = new Message
            {
                SessionId = id,
                Role = dto.Role,
                Content = dto.Content,
                ToolCallId = dto.ToolCallId
            };

            _context.Messages.Add(message);
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessages), new { id }, message);
        }
    }
}
