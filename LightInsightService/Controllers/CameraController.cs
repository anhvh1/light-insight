using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceBUS;

namespace LightInsightService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {

        private readonly CameraServiceBUS _service;

        public CameraController(CameraServiceBUS service)
        {
            _service = service;
        }

        [HttpGet("uri")]
        public IActionResult GetUri()
        {
            var uri = _service.LoadCameraUriMap();
            return Ok(uri);
        }

    }
}
