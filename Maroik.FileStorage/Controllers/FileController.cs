using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using Maroik.FileStorage.Contracts;

namespace Maroik.FileStorage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IClamavRepository _clamavRepository;

        public FileController(IClamavRepository clamavRepository)
        {
            _clamavRepository = clamavRepository;
        }
        
        [AllowAnonymous]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAsync([FromForm] IFormFile file, [FromForm] string filePath)
        {
            try
            {
                if (file is not { Length: > 0 })
                    return BadRequest("Invalid file.");

                // 파일 이름 추출 및 경로 보호
                var fileName = Path.GetFileName(file.FileName);
                var safeFilePath = Path.Combine(filePath, fileName);

                // ClamAV 검사 전 메모리 스트림 복사
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                if (!await _clamavRepository.ScanWithClamavAsync(memoryStream))
                    return BadRequest("File may be infected with a virus.");

                // 디렉터리 없으면 생성
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                // 디스크 저장
#pragma warning disable SCS0018
                await using (var stream = new FileStream(safeFilePath, FileMode.Create))
#pragma warning restore SCS0018
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(stream);
                    await stream.FlushAsync();
                }

                // 파일 권한 설정 (644)
                var chmod = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
#pragma warning disable SCS0001
                    Arguments = $"644 \"{safeFilePath}\"",
#pragma warning restore SCS0001
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(chmod))
                {
                    await process?.WaitForExitAsync()!;
                }

                return Ok("File upload and execution permissions removed.");
            }
            catch
            {
                return BadRequest("File upload failed.");
            }
        }

        [AllowAnonymous]
        [HttpPost("download")]
        public async Task<IActionResult> DownloadAsync([FromForm] string filePath)
        {
            try
            {
                // 파일 이름에서 디렉터리 경로 제거 (보안)
                var sanitizedFileName = Path.GetFileName(filePath);
                filePath = Path.Combine(Path.GetDirectoryName(filePath ?? "") ?? string.Empty, sanitizedFileName);

                // 파일이 존재하는지 확인
                if (!System.IO.File.Exists(filePath)) return NotFound("File not found");
#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found.
                var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found.

                // 파일 확장자에 맞는 Content-Type을 가져오기 위한 FileExtensionContentTypeProvider 사용
                var provider = new FileExtensionContentTypeProvider();

                // contentType 설정 (없을 경우 기본값 "application/octet-stream" 설정)
                if (!provider.TryGetContentType(sanitizedFileName, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return File(fileData, contentType, sanitizedFileName);

            }
            catch
            {
                return BadRequest("Invalid file");
            }
        }

        [AllowAnonymous]
        [HttpPost("delete")]
        public IActionResult Delete([FromForm] string filePath)
        {
            // 파일 이름에서 디렉터리 경로 제거 (보안)
            var sanitizedFileName = Path.GetFileName(filePath);
            filePath = Path.Combine(Path.GetDirectoryName(filePath ?? "") ?? string.Empty, sanitizedFileName);

            if (!System.IO.File.Exists(filePath)) return NotFound();
#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found.
            System.IO.File.Delete(filePath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found.
            return Ok();

        }
    }
}