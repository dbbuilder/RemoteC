using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileTransferController : ControllerBase
    {
        private readonly IFileTransferService _fileTransferService;
        private readonly ILogger<FileTransferController> _logger;

        public FileTransferController(
            IFileTransferService fileTransferService,
            ILogger<FileTransferController> logger)
        {
            _fileTransferService = fileTransferService;
            _logger = logger;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateTransfer([FromBody] FileTransferRequest request)
        {
            try
            {
                var transfer = await _fileTransferService.InitiateTransferAsync(request);
                return Ok(transfer);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid file transfer request");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating file transfer");
                return StatusCode(500, new { error = "An error occurred while initiating the transfer" });
            }
        }

        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromBody] FileChunk chunk)
        {
            try
            {
                var result = await _fileTransferService.UploadChunkAsync(chunk);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid chunk upload request");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading chunk");
                return StatusCode(500, new { error = "An error occurred while uploading the chunk" });
            }
        }

        [HttpGet("{transferId}/status")]
        public async Task<IActionResult> GetTransferStatus(Guid transferId)
        {
            try
            {
                var status = await _fileTransferService.GetTransferStatusAsync(transferId);
                return Ok(status);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Transfer not found: {TransferId}", transferId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfer status");
                return StatusCode(500, new { error = "An error occurred while getting transfer status" });
            }
        }

        [HttpGet("{transferId}/download/{chunkNumber}")]
        public async Task<IActionResult> DownloadChunk(Guid transferId, int chunkNumber)
        {
            try
            {
                var chunk = await _fileTransferService.DownloadChunkAsync(transferId, chunkNumber);
                if (chunk == null)
                {
                    return NotFound(new { error = "Chunk not found" });
                }
                return Ok(chunk);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error downloading chunk");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading chunk");
                return StatusCode(500, new { error = "An error occurred while downloading the chunk" });
            }
        }

        [HttpPost("{transferId}/cancel")]
        public async Task<IActionResult> CancelTransfer(Guid transferId)
        {
            try
            {
                await _fileTransferService.CancelTransferAsync(transferId);
                return Ok(new { message = "Transfer cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transfer");
                return StatusCode(500, new { error = "An error occurred while cancelling the transfer" });
            }
        }

        [HttpPost("cleanup-stalled")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> CleanupStalledTransfers([FromQuery] int stalledMinutes = 60)
        {
            try
            {
                var cleaned = await _fileTransferService.CleanupStalledTransfersAsync();
                return Ok(new { cleanedTransfers = cleaned });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up stalled transfers");
                return StatusCode(500, new { error = "An error occurred while cleaning up stalled transfers" });
            }
        }
    }
}