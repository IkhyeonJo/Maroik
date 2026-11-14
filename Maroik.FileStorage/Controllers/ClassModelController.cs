using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.FileStorage.Controllers;

/// <summary>
/// Internal API of the class-suggestion model files, next to <see cref="FileController"/> (the attachments) and
/// <see cref="FileManagerController"/>: <c>save</c> stores a model the worker trained at <c>upload/ClassModels/{guid}.zip</c> (no virus
/// scan: no user uploaded it) and <c>delete</c> removes the model a retraining replaced. A model is read through
/// <c>api/File/download</c> like any stored file. Like the rest of this service it is reachable only on the internal network. A refusal is
/// answered with its <c>FileStorageRefusals</c> text and logged with the path and the caller's correlation id.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClassModelController(IClassModelStorageService storage, ILogger<ClassModelController> logger) : ControllerBase
{
    /// <summary>The fixed answer to a request that failed inside the service (the exception's text never leaves the server).</summary>
    private const string RequestFailed = "The request failed.";

    /// <summary>Saves <paramref name="file"/> at <paramref name="filePath"/>: 200, 409 when a file is there already, or 400.</summary>
    [AllowAnonymous]
    [HttpPost("save")]
    public async Task<IActionResult> SaveAsync([FromForm] string? filePath, IFormFile? file, [FromForm] string? correlationId = null)
    {
        try
        {
            string? error;
            long length = file?.Length ?? 0;
            if (file == null)
            {
                error = FileStorageRefusals.InvalidUpload;
            }
            else
            {
                await using Stream content = file.OpenReadStream();
                error = await storage.SaveAsync(filePath, content, HttpContext.RequestAborted);
            }

            if (error == null)
            {
                logger.LogInformation("Class model saved: {FilePath} ({Length} bytes). CorrelationId={CorrelationId}", filePath, length, correlationId);
                return Ok();
            }

            logger.LogWarning("Class model save refused: {Reason}. FilePath={FilePath} CorrelationId={CorrelationId}", error, filePath, correlationId);
            return error == FileStorageRefusals.FileExists ? Conflict(error) : BadRequest(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Class model save failed. FilePath={FilePath} CorrelationId={CorrelationId}", filePath, correlationId);
            return BadRequest(RequestFailed);
        }
    }

    /// <summary>Deletes the model file at <paramref name="filePath"/>: 200, 404 when it is not there, or 400.</summary>
    [AllowAnonymous]
    [HttpPost("delete")]
    public IActionResult Delete([FromForm] string? filePath, [FromForm] string? correlationId = null)
    {
        try
        {
            string? error = storage.Delete(filePath);
            if (error == null)
            {
                logger.LogInformation("Class model deleted: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
                return Ok();
            }

            logger.LogWarning("Class model delete refused: {Reason}. FilePath={FilePath} CorrelationId={CorrelationId}", error, filePath, correlationId);
            return error == FileStorageRefusals.FileNotFound ? NotFound(error) : BadRequest(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Class model delete failed. FilePath={FilePath} CorrelationId={CorrelationId}", filePath, correlationId);
            return BadRequest(RequestFailed);
        }
    }
}
