using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentFree.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToolsController> _logger;

        public ToolsController(AppDbContext context, ILogger<ToolsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/tools - 获取所有工具
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tool>>> GetTools()
        {
            return await _context.Tools
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        // GET /api/tools/{id} - 获取单个工具
        [HttpGet("{id}")]
        public async Task<ActionResult<Tool>> GetTool(string id)
        {
            var tool = await _context.Tools.FindAsync(id);
            if (tool == null) return NotFound();
            return tool;
        }

        // POST /api/tools - 注册工具
        [HttpPost]
        public async Task<ActionResult<Tool>> CreateTool([FromBody] CreateToolDto dto)
        {
            var tool = new Tool
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                ParametersSchema = dto.ParametersSchema
            };

            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTool), new { id = tool.Id }, tool);
        }

        // DELETE /api/tools/{id} - 删除工具
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTool(string id)
        {
            var tool = await _context.Tools.FindAsync(id);
            if (tool == null) return NotFound();

            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
