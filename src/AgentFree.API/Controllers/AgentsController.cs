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
                agent.AgentType,
                agent.ServiceUrl,
                agent.AgentId,
                agent.Token,
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
                AgentType = dto.AgentType ?? "Goldfish",
                ServiceUrl = dto.ServiceUrl,
                AgentId = dto.AgentId,
                Token = dto.Token,
                SystemPrompt = dto.SystemPrompt,
                Status = "Inactive",
                // 对话大模型字段
                LLMProvider = dto.LLMProvider,
                LLMBaseUrl = dto.LLMBaseUrl,
                LLMModelName = dto.LLMModelName,
                LLMApiKey = dto.LLMApiKey
            };
            _ctx.Agents.Add(agent);
            await _ctx.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, new {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.AgentType,
                agent.ServiceUrl,
                agent.AgentId,
                agent.Token,
                agent.SystemPrompt,
                agent.LLMProvider,
                agent.LLMBaseUrl,
                agent.LLMModelName,
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
            if (!string.IsNullOrEmpty(dto.AgentType)) agent.AgentType = dto.AgentType;
            if (dto.SystemPrompt != null) agent.SystemPrompt = dto.SystemPrompt;
            if (!string.IsNullOrEmpty(dto.ServiceUrl)) agent.ServiceUrl = dto.ServiceUrl;
            if (!string.IsNullOrEmpty(dto.AgentId)) agent.AgentId = dto.AgentId;
            if (!string.IsNullOrEmpty(dto.Token)) agent.Token = dto.Token;
            if (!string.IsNullOrEmpty(dto.Status)) agent.Status = dto.Status;
            // 对话大模型字段
            if (!string.IsNullOrEmpty(dto.LLMProvider)) agent.LLMProvider = dto.LLMProvider;
            if (!string.IsNullOrEmpty(dto.LLMBaseUrl)) agent.LLMBaseUrl = dto.LLMBaseUrl;
            if (!string.IsNullOrEmpty(dto.LLMModelName)) agent.LLMModelName = dto.LLMModelName;
            if (!string.IsNullOrEmpty(dto.LLMApiKey)) agent.LLMApiKey = dto.LLMApiKey;
            agent.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return Ok(new {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.SystemPrompt,
                agent.AgentType,
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
