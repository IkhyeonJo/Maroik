using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.Website.Extensions;

/// <summary>
/// The editor (Summernote) image upload the three editor endpoints share (Forum, Management, Calendar): the same refusals, the
/// same logging and the same reply. Each controller hands in its own logger and localizer, so a message still comes from that
/// controller's resx pair and a log entry from its category, and its own call into its own service.
/// </summary>
public static class EditorImageUploadExtensions
{
    extension(Controller controller)
    {
        /// <summary>Hands <paramref name="imageFile"/> to <paramref name="upload"/>, which checks and stores it, and answers the editor.</summary>
        /// <param name="imageFile">The posted file, null when the request carried none.</param>
        /// <param name="maxBytes">The largest body read into memory (the service refuses anything larger).</param>
        /// <param name="localizer">The calling controller's localizer.</param>
        /// <param name="logger">The calling controller's logger.</param>
        /// <param name="dataProtection">Encrypts the storage path returned to the editor.</param>
        /// <param name="upload">The controller's service call, given the file, the uploader's e-mail and the token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IActionResult> UploadEditorImageAsync(
            IFormFile? imageFile,
            long maxBytes,
            IHtmlLocalizer localizer,
            ILogger logger,
            IDataProtectionService dataProtection,
            Func<AttachedFileDto, string, CancellationToken, Task<SummernoteUploadResult>> upload,
            CancellationToken ct)
        {
            if (imageFile == null)
                return controller.Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });

            // The attachment service checks the size, the extension, the content type and the content, and logs a refusal with
            // the uploader. A body over the size limit is not read into memory: it reaches the service with its size only.
            string uploaderEmail = controller.HttpContext.GetLoggedInEmail();
            SummernoteUploadResult uploadResult;
            try
            {
                AttachedFileDto file = (await imageFile.ToAttachedFileInfoAsync(maxBytes, ct))!;
                uploadResult = await upload(file, uploaderEmail, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Editor image upload failed for {Email}", uploaderEmail);
                return controller.Ok(new { result = false, errorMessage = localizer[ServiceResult.TemporaryErrorKey].Value });
            }

            if (!uploadResult.Success)
                return controller.Ok(new { result = false, errorMessage = localizer[uploadResult.ErrorKey ?? "Input is invalid", uploadResult.ErrorArgs].ToPlainString() });

            // The Summernote editor only needs the raw bytes (base64) + content type to build an object
            // URL for the inserted <img>; return those explicitly rather than serializing a whole
            // FileContentResult and relying on its property names.
            return controller.Ok(new
            {
                result = true,
                file = new
                {
                    fileContents = Convert.ToBase64String(uploadResult.FileBytes!),
                    contentType = uploadResult.ContentType!
                },
                filePath = dataProtection.Protect(ProtectedValue.StoredFilePath, uploadResult.FilePath!)
            });
        }
    }
}
