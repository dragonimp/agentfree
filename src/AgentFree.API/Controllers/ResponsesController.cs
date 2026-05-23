using AgentFree.API.Models.Response;
using AgentFree.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentFree.API.Controllers;

/// <summary>
/// OpenAI Responses API 兼容控制器
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class ResponsesController : ControllerBase
{
    private readonly IResponseService _responseService;
    private readonly ILogger<ResponsesController> _logger;

    public ResponsesController(
        IResponseService responseService,
        ILogger<ResponsesController> logger)
    {
        _responseService = responseService;
        _logger = logger;
    }

    // POST /v1/responses - 创建新响应
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateResponseRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Stream)
            {
                // 流式响应 - 返回SSE格式（暂不支持）
                _logger.LogWarning("Stream mode not yet implemented, falling back to non-stream");
            }

            // 标准响应（stream/fallback都走这里）
            var response = await _responseService.ExecuteAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response creation failed");
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }

    // GET /v1/responses/{id} - 获取响应详情
    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken ct)
    {
        try
        {
            var response = await _responseService.GetAsync(id, ct);
            if (response == null)
                return NotFound(new { error = new { message = "Response not found" } });
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response retrieval failed: {Id}", id);
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }

    // POST /v1/responses/{id}/cancel - 取消响应
    [HttpPost("{id}/cancel")]
    [Produces("application/json")]
    public async Task<IActionResult> CancelAsync(string id, CancellationToken ct)
    {
        try
        {
            await _responseService.CancelAsync(id, ct);
            return Ok(new { id, status = "cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response cancellation failed: {Id}", id);
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }
}
