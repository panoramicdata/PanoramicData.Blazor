using System.Net;

namespace PanoramicData.Blazor.WebAssembly.Server.Controllers;

/// <summary>
/// API controller that serves file download and upload operations for the demo application.
/// </summary>
[ApiController]
[Route("[controller]")]
public class FilesController : Controller
{
	/// <summary>
	/// Downloads a demo file. Returns embedded Markdown content for <c>.md</c> paths, or a WebM video otherwise.
	/// </summary>
	/// <param name="path">The requested file path; used to determine the content type.</param>
	/// <returns>A <see cref="FileStreamResult"/> containing the demo file content.</returns>
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
			var stream = typeof(Demo.Data.Person).Assembly.GetManifestResourceStream($"PanoramicData.Blazor.Demo.TestVideo.webm");
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

	/// <summary>
	/// Accepts a multipart form-data file upload and saves it to the server's temp directory.
	/// </summary>
	/// <param name="uploadInfo">Form fields and file payload for the upload.</param>
	/// <returns>An HTTP 200 OK response on success.</returns>
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

	/// <summary>Holds the form fields and uploaded file for an upload request.</summary>
	public class FileUploadModel
	{
		/// <summary>Gets or sets the unique upload key.</summary>
		public string? Key { get; set; }
		/// <summary>Gets or sets the destination path for the uploaded file.</summary>
		public string? Path { get; set; }
		/// <summary>Gets or sets the uploaded file.</summary>
		public IFormFile? File { get; set; }
	}
}

