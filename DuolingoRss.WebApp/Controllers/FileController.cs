using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Web;
using Microsoft.Extensions.Logging;

namespace DuolingoRss.WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly string _basePath = "E:/Data/PodCasts/DuolingoApp";
        private readonly ILogger<FileController> _logger;

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{foldername}/{fileName}")]
        public IActionResult GetFile(string folderName,string fileName)
        {
            try
            {
                // Decode the file name to handle special characters
                var decodedFileName = HttpUtility.UrlDecode(folderName) + "//" + HttpUtility.UrlDecode(fileName);
                var filePath = Path.Combine(_basePath, decodedFileName);

                _logger.LogInformation($"Attempting to retrieve file: {filePath}");

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    return NotFound();
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "audio/mpeg", decodedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file");
                return StatusCode(500, "An error occurred while retrieving the file.");
            }
        }
    }
}