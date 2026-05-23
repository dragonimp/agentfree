using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentFree.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly AppDbContext _ctx;

        public AgentsController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents()
        {
            var agents = await _ctx.Agents.OrderBy(a => a.Id).ToListAsync();
            return Ok(agents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAgent(int id)
        {
            var agent = await _ctx.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            return Ok(new {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.SystemPrompt,
                agent.Status,
                agent.CreatedAt,
                agent.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentDto dto)
        {
            var agent = new Agent
            {
                Name = dto.Name,
                Description = dto.Description,
                SystemPrompt = dto.SystemPrompt,
                Status = "Inactive"
            };
            _ctx.Agents.Add(agent);
            await _ctx.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, new {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.SystemPrompt,
                agent.Status,
                agent.CreatedAt,
                agent.UpdatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAgent(int id, [FromBody] UpdateAgentDto dto)
        {
            var agent = await _ctx.Agents.FindAsync(id);
            if (agent == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Name)) agent.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) agent.Description = dto.Description;
            if (dto.SystemPrompt != null) agent.SystemPrompt = dto.SystemPrompt;
            if (!string.IsNullOrEmpty(dto.Status)) agent.Status = dto.Status;
            agent.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return Ok(new {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.SystemPrompt,
                agent.Status,
                agent.CreatedAt,
                agent.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgent(int id)
        {
            var agent = await _ctx.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            _ctx.Agents.Remove(agent);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartAgent(int id)
        {
            var agent = await _ctx.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            agent.Status = "Active";
            agent.UpdatedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
            return Ok(new { agent.Id, agent.Status });
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopAgent(int id)
        {
            var agent = await _ctx.Agents.FindAsync(id);
            if (agent == null) return NotFound();
            agent.Status = "Inactive";
            agent.UpdatedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
            return Ok(new { agent.Id, agent.Status });
        }
    }
}
