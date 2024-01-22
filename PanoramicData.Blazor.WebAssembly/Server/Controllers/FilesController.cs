using System.Net;

namespace PanoramicData.Blazor.WebAssembly.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class FilesController : Controller
{
	[HttpGet("download")]
	public IActionResult Download(string path)
	{
		// markdown file?
		if (Path.GetExtension(path) == ".md")
		{
			var stream = typeof(Demo.Data.Person).Assembly.GetManifestResourceStream($"PanoramicData.Blazor.Demo.TestMarkdown.md");
			if (stream is null)
			{
				return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
			}
			return new FileStreamResult(stream, "text/markdown")
			{
				FileDownloadName = Path.GetFileName(path)
			};
		}
		else
		{
			var stream = typeof(Demo.Data.Person).Assembly.GetManifestResourceStream($"PanoramicData.Blazor.Demo.file_example_WEBM_1920_3_7MB.webm");
			if (stream is null)
			{
				return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
			}
			return new FileStreamResult(stream, "text/plain")
			{
				FileDownloadName = $"{Path.GetFileNameWithoutExtension(path)}.webm"
			};
		}
	}

	[HttpPost("upload")]
	[RequestSizeLimit(1000000000)] // 1 GB
	[RequestFormLimits(MultipartBodyLengthLimit = 1000000000)]
	public async Task<IActionResult> Upload([FromForm] FileUploadModel uploadInfo)
	{
		Console.WriteLine($"Upload: Key = {uploadInfo.Key}");
		if (uploadInfo.File != null)
		{
			var filePath = Path.Combine("C:", "Temp", "Uploads", uploadInfo.File.FileName);
			using var stream = System.IO.File.Create(filePath);
			await uploadInfo.File.CopyToAsync(stream);
		}

		return Ok();
	}

	public class FileUploadModel
	{
		public string Key { get; set; }
		public string Path { get; set; }
		public IFormFile File { get; set; }
	}
}

