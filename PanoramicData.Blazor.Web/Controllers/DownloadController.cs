using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace PanoramicData.Blazor.Web.Controllers
{
	[Route("files/[controller]")]
	[ApiController]
	public class DownloadController : Controller
	{
		[HttpGet("[action]")]
		public IActionResult DownloadFile(string path)
		{
			var stream = new FileStream("Download/file_example_WEBM_1920_3_7MB.webm", FileMode.Open);
			var result = new FileStreamResult(stream, "text/plain");
			result.FileDownloadName = $"{Path.GetFileNameWithoutExtension(path)}.webm";
			return result;
		}
	}
}

