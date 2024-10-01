using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsync([FromForm] IFormFile file, [FromForm] string filePath)
    {
        try
        {
            if (file.Length > 0)
            {
                if (!Directory.Exists(filePath))
                {
                    _ = Directory.CreateDirectory(filePath);
                }

                filePath = Path.Combine(filePath, file.FileName);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                using (var stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                {
                    await file.CopyToAsync(stream);
                    await stream.FlushAsync();
                }

                //// 파일 권한 설정
                //var fileInfo = new FileInfo(filePath);
                //var acl = fileInfo.GetAccessControl();
                //acl.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.ExecuteFile, AccessControlType.Deny));
                //fileInfo.SetAccessControl(acl);

                return Ok();
            }
            else
            {
                return BadRequest("Invalid file");
            }
        }
        catch
        {
            return BadRequest("Invalid file");
        }
    }

    [AllowAnonymous]
    [HttpPost("download")]
    public async Task<IActionResult> DownloadAsync([FromForm] string filePath)
    {
        try
        {
            // 파일 이름에서 디렉터리 경로 제거 (보안)
            string sanitizedFileName = Path.GetFileName(filePath);
            filePath = Path.Combine(Path.GetDirectoryName(filePath ?? ""), sanitizedFileName);

            // 파일이 존재하는지 확인
            if (System.IO.File.Exists(filePath))
            {
#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found.
                byte[] fileData = await System.IO.File.ReadAllBytesAsync(filePath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found.

                return File(fileData, "application/octet-stream", sanitizedFileName);
            }

            return NotFound("File not found");
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
        string sanitizedFileName = Path.GetFileName(filePath);
        filePath = Path.Combine(Path.GetDirectoryName(filePath ?? ""), sanitizedFileName);

        if (System.IO.File.Exists(filePath))
        {
#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found.
            System.IO.File.Delete(filePath);
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found.
            return Ok();
        }

        return NotFound();
    }
}
