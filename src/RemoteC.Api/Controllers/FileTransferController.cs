using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// Manages file transfers between remote devices
    /// </summary>
    /// <remarks>
    /// The FileTransferController provides endpoints for:
    /// - Initiating file transfers
    /// - Uploading and downloading file chunks
    /// - Monitoring transfer status
    /// - Cancelling active transfers
    /// - Administrative cleanup of stalled transfers
    /// All endpoints require authentication. File transfers use a chunked approach for reliability.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Initiates a new file transfer
        /// </summary>
        /// <param name="request">File transfer request details including filename, size, and session</param>
        /// <returns>Transfer details including transfer ID and chunk information</returns>
        /// <remarks>
        /// This endpoint creates a new file transfer record and returns the transfer ID and chunk parameters.
        /// The client should use this information to upload file chunks sequentially.
        /// </remarks>
        /// <response code="200">Transfer successfully initiated</response>
        /// <response code="400">Invalid request parameters</response>
        [HttpPost("initiate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Uploads a file chunk
        /// </summary>
        /// <param name="chunk">File chunk containing transfer ID, chunk number, and data</param>
        /// <returns>Chunk upload result including progress information</returns>
        /// <remarks>
        /// Chunks must be uploaded in sequential order. The server tracks received chunks
        /// and validates chunk integrity using checksums.
        /// </remarks>
        /// <response code="200">Chunk successfully uploaded</response>
        /// <response code="400">Invalid chunk data or out of sequence</response>
        [HttpPost("upload-chunk")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Gets the current status of a file transfer
        /// </summary>
        /// <param name="transferId">Unique identifier of the file transfer</param>
        /// <returns>Transfer status including progress and completion state</returns>
        /// <response code="200">Returns the transfer status</response>
        /// <response code="404">Transfer not found</response>
        [HttpGet("{transferId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Downloads a specific file chunk
        /// </summary>
        /// <param name="transferId">Unique identifier of the file transfer</param>
        /// <param name="chunkNumber">The chunk number to download (0-based)</param>
        /// <returns>The requested file chunk data</returns>
        /// <remarks>
        /// This endpoint is used by receiving clients to download file chunks sequentially.
        /// Each chunk contains the actual file data and metadata.
        /// </remarks>
        /// <response code="200">Returns the requested chunk</response>
        /// <response code="400">Invalid chunk number</response>
        /// <response code="404">Transfer or chunk not found</response>
        [HttpGet("{transferId}/download/{chunkNumber}")]
        [ProducesResponseType(typeof(FileChunk), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Cancels an active file transfer
        /// </summary>
        /// <param name="transferId">Unique identifier of the file transfer to cancel</param>
        /// <returns>Confirmation message</returns>
        /// <remarks>
        /// This endpoint cancels an in-progress transfer and cleans up any temporary data.
        /// Once cancelled, a transfer cannot be resumed.
        /// </remarks>
        /// <response code="200">Transfer successfully cancelled</response>
        [HttpPost("{transferId}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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

        /// <summary>
        /// Cleans up stalled file transfers (Admin only)
        /// </summary>
        /// <param name="stalledMinutes">Time in minutes after which a transfer is considered stalled (default: 60)</param>
        /// <returns>Number of transfers cleaned up</returns>
        /// <remarks>
        /// This administrative endpoint identifies and removes transfers that have been inactive
        /// for the specified duration. This helps maintain system health and free up resources.
        /// Requires admin role.
        /// </remarks>
        /// <response code="200">Returns the number of cleaned transfers</response>
        /// <response code="403">User doesn't have admin role</response>
        [HttpPost("cleanup-stalled")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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