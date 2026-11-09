using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
}
