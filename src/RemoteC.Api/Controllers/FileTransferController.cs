using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// Controller for file transfer operations
    /// </summary>
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
            _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates a new file transfer
        /// </summary>
        /// <param name="request">File transfer request</param>
        /// <returns>Transfer details</returns>
        [HttpPost("initiate")]
        public async Task<ActionResult<FileTransfer>> InitiateTransfer([FromBody] FileTransferRequest request)
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
                _logger.LogError(ex, "Failed to initiate file transfer");
                return StatusCode(500, new { error = "Failed to initiate file transfer" });
            }
        }

        /// <summary>
        /// Uploads a file chunk
        /// </summary>
        /// <param name="chunk">File chunk data</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload-chunk")]
        public async Task<ActionResult<ChunkUploadResult>> UploadChunk([FromBody] FileChunk chunk)
        {
            try
            {
                var result = await _fileTransferService.UploadChunkAsync(chunk);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload chunk for transfer {TransferId}", chunk.TransferId);
                return StatusCode(500, new { error = "Failed to upload chunk" });
            }
        }

        /// <summary>
        /// Gets the status of a file transfer
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <returns>Transfer status</returns>
        [HttpGet("{transferId}/status")]
        public async Task<ActionResult<FileTransfer>> GetTransferStatus(Guid transferId)
        {
            var transfer = await _fileTransferService.GetTransferStatusAsync(transferId);
            if (transfer == null)
            {
                return NotFound();
            }
            return Ok(transfer);
        }

        /// <summary>
        /// Gets missing chunks for a transfer
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <returns>List of missing chunk indices</returns>
        [HttpGet("{transferId}/missing-chunks")]
        public async Task<ActionResult<IEnumerable<int>>> GetMissingChunks(Guid transferId)
        {
            try
            {
                var missingChunks = await _fileTransferService.GetMissingChunksAsync(transferId);
                return Ok(missingChunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get missing chunks for transfer {TransferId}", transferId);
                return StatusCode(500, new { error = "Failed to get missing chunks" });
            }
        }

        /// <summary>
        /// Downloads a file chunk
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <param name="chunkIndex">Chunk index</param>
        /// <returns>Chunk data</returns>
        [HttpGet("{transferId}/download-chunk/{chunkIndex}")]
        public async Task<ActionResult<FileChunk>> DownloadChunk(Guid transferId, int chunkIndex)
        {
            try
            {
                var chunk = await _fileTransferService.DownloadChunkAsync(transferId, chunkIndex);
                if (chunk == null)
                {
                    return NotFound();
                }
                return Ok(chunk);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download chunk {ChunkIndex} for transfer {TransferId}", 
                    chunkIndex, transferId);
                return StatusCode(500, new { error = "Failed to download chunk" });
            }
        }

        /// <summary>
        /// Cancels a file transfer
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{transferId}/cancel")]
        public async Task<ActionResult<bool>> CancelTransfer(Guid transferId)
        {
            try
            {
                var result = await _fileTransferService.CancelTransferAsync(transferId);
                if (!result)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel transfer {TransferId}", transferId);
                return StatusCode(500, new { error = "Failed to cancel transfer" });
            }
        }

        /// <summary>
        /// Gets transfer metrics
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <returns>Transfer metrics</returns>
        [HttpGet("{transferId}/metrics")]
        public async Task<ActionResult<FileTransferMetrics>> GetTransferMetrics(Guid transferId)
        {
            try
            {
                var metrics = await _fileTransferService.GetTransferMetricsAsync(transferId);
                return Ok(metrics);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics for transfer {TransferId}", transferId);
                return StatusCode(500, new { error = "Failed to get transfer metrics" });
            }
        }

        /// <summary>
        /// Cleans up expired transfers
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("cleanup/expired")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult> CleanupExpiredTransfers()
        {
            try
            {
                await _fileTransferService.CleanupExpiredTransfersAsync();
                return Ok(new { message = "Expired transfers cleaned up successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired transfers");
                return StatusCode(500, new { error = "Failed to cleanup expired transfers" });
            }
        }

        /// <summary>
        /// Cleans up stalled transfers
        /// </summary>
        /// <returns>Number of stalled transfers cleaned</returns>
        [HttpPost("cleanup/stalled")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<int>> CleanupStalledTransfers()
        {
            try
            {
                var count = await _fileTransferService.CleanupStalledTransfersAsync();
                return Ok(new { cleanedCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup stalled transfers");
                return StatusCode(500, new { error = "Failed to cleanup stalled transfers" });
            }
        }
    }
}