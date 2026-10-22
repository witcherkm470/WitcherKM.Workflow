using Microsoft.AspNetCore.Mvc;
using WitcherKM.Workflow.Api.Contracts;
using WitcherKM.Workflow.Application;

namespace WitcherKM.Workflow.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController(IImageStorage images) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<ActionResult<ImageUploadResponse>> Upload([FromForm] IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0 || image.Length > 10 * 1024 * 1024)
            throw new WorkflowException(400, "Размер изображения должен быть от 1 байта до 10 МБ.");
        await using var stream = image.OpenReadStream();
        var url = await images.Save(stream, image.ContentType, ct);
        return Ok(new ImageUploadResponse(url));
    }

    [HttpGet("{fileName}")]
    public IActionResult Get(string fileName)
    {
        var image = images.Get(fileName);
        return image is null ? NotFound() : PhysicalFile(image.Value.Path, image.Value.ContentType);
    }
}
