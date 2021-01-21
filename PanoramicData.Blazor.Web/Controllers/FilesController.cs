using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
			public string Key { get; set; } = string.Empty;
			public string Path { get; set; } = string.Empty;
			public IFormFile? File { get; set; }
		}
	}
}

