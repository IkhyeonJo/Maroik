using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.AccessControl;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly string _uploadPath = "/app/upload";

    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string param1, [FromForm] string param2)
    {
        try
        {
            if (file.Length > 0)
            {
                var filePath = Path.Combine(_uploadPath, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                Directory.CreateDirectory("/app/upload/hello"); // example code

                //// 파일 권한 설정
                //var fileInfo = new FileInfo(filePath);
                //var acl = fileInfo.GetAccessControl();
                //acl.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.ExecuteFile, AccessControlType.Deny));
                //fileInfo.SetAccessControl(acl);

                return Ok();
            }
        }
        catch (Exception ex)
        {

        }

        return BadRequest("Invalid file");
    }

    [AllowAnonymous]
    [HttpPost("download")]
    public IActionResult Download(string fileName)
    {
        var filePath = Path.Combine(_uploadPath, fileName);

        if (System.IO.File.Exists(filePath))
        {
            var fileData = System.IO.File.ReadAllBytes(filePath);
            return File(fileData, "application/octet-stream", fileName);
        }

        return NotFound();
    }

    [AllowAnonymous]
    [HttpPost("delete")]
    public IActionResult Delete(string fileName)
    {
        var filePath = Path.Combine(_uploadPath, fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            return NoContent();
        }

        return NotFound();
    }
}
