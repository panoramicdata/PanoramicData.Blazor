using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace PanoramicData.Blazor.Web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class FilesController : Controller
	{
		[HttpGet("[action]")]
		public IActionResult Download(string path)
		{
			var stream = new FileStream("Download/file_example_WEBM_1920_3_7MB.webm", FileMode.Open);
			var result = new FileStreamResult(stream, "text/plain");
			result.FileDownloadName = $"{Path.GetFileNameWithoutExtension(path)}.webm";
			return result;
		}

		[HttpPost("[action]")]
		[RequestSizeLimit(1000000000)] // 1 GB
		[RequestFormLimits(MultipartBodyLengthLimit = 1000000000)]
		public async Task<IActionResult> Upload([FromForm] FileUploadModel uploadInfo)
		{
			//long size = files.Sum(f => f.Length);
			//foreach (var formFile in files)
			//{
			if (uploadInfo.File != null)
			{
				var filePath = Path.Combine("C:", "Temp", "Uploads", uploadInfo.File.FileName);
				using (var stream = System.IO.File.Create(filePath))
				{
					await uploadInfo.File.CopyToAsync(stream);
				}
			}
			//}
			//// Process uploaded files
			//// Don't rely on or trust the FileName property without validation.
			//return Ok(new { count = files.Count, size });

			return Ok();
		}

		public class FileUploadModel
		{
			public string Path { get; set; }
			public IFormFile File { get; set; }
		}
	}
}

