using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class FilesController : Controller
	{
		[HttpGet("download")]
		public IActionResult Download(string path)
		{
			var stream = typeof(Demo.Data.Person).Assembly.GetManifestResourceStream($"PanoramicData.Blazor.Demo.file_example_WEBM_1920_3_7MB.webm");
			if (stream is null)
			{
				return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
			}
			var result = new FileStreamResult(stream, "text/plain")
			{
				FileDownloadName = $"{Path.GetFileNameWithoutExtension(path)}.webm"
			};
			return result;
		}

		[HttpPost("upload")]
		[RequestSizeLimit(1000000000)] // 1 GB
		[RequestFormLimits(MultipartBodyLengthLimit = 1000000000)]
		public async Task<IActionResult> Upload([FromForm] FileUploadModel uploadInfo)
		{
			Console.WriteLine($"Upload: Key = {uploadInfo.Key}, Path = {uploadInfo.Path}, Name = {uploadInfo.Name}, Session = {uploadInfo.SessionId}");

			// upload to temp folder - maintaining folder structure
			if (uploadInfo.File != null)
			{
				var folderPath = string.IsNullOrWhiteSpace(uploadInfo.Path.TrimStart('/'))
					? "C:\\Temp\\Uploads"
					: Path.Combine("C:\\Temp\\Uploads", uploadInfo.Path.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				var filePath = Path.Combine(folderPath, uploadInfo.Name);
				using var stream = System.IO.File.Create(filePath);
				await uploadInfo.File.CopyToAsync(stream);
			}
			return Ok();
		}

		public class FileUploadModel
		{
			public string Key { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public string Path { get; set; } = string.Empty;
			public string SessionId { get; set; } = string.Empty;
			public IFormFile? File { get; set; }
		}
	}
}

