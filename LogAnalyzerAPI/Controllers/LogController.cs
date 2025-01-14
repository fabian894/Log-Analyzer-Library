using Microsoft.AspNetCore.Mvc;
using LogAnalyzerLibrary;
using System;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly LogProcessor _logProcessor;
    private readonly IWebHostEnvironment _environment;

    public LogController(IWebHostEnvironment environment)
    {
        _environment = environment;
        _logProcessor = new LogProcessor();
    }

    [HttpGet("count-errors")]
    public IActionResult CountErrors([FromQuery] string directoryPath)
    {
        try
        {
            var errorCounts = _logProcessor.CountErrors(directoryPath);
            return Ok(errorCounts);
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpGet("count-unique-errors")]
    public IActionResult CountUniqueErrors([FromQuery] string directoryPath)
    {
        try
        {
            var uniqueErrors = _logProcessor.CountUniqueErrors(directoryPath);
            return Ok(uniqueErrors);
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpGet("count-duplicated-errors")]
    public IActionResult CountDuplicatedErrors([FromQuery] string directoryPath)
    {
        try
        {
            var duplicateCounts = _logProcessor.CountDuplicatedErrors(directoryPath);
            return Ok(duplicateCounts);
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpDelete("delete-archives")]
    public IActionResult DeleteArchivesFromPeriod([FromQuery] string directoryPath, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            _logProcessor.DeleteArchivesFromPeriod(directoryPath, startDate, endDate);
            return Ok(new { message = "Archives deleted successfully within the specified period." });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpPost("archive-logs")]
    public IActionResult ArchiveLogsFromPeriod([FromQuery] string directoryPath, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            _logProcessor.ArchiveLogsFromPeriod(directoryPath, startDate, endDate);
            return Ok(new { message = "Logs archived successfully.", archivedFile = $"{startDate:dd_MM_yyyy}-{endDate:dd_MM_yyyy}.zip" });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpDelete("delete-logs")]
    public IActionResult DeleteLogsFromPeriod([FromQuery] string directoryPath, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            _logProcessor.DeleteLogsFromPeriod(directoryPath, startDate, endDate);
            return Ok(new { message = "Log files deleted successfully." });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpGet("count-logs-in-period")]
    public IActionResult CountLogsInPeriod([FromQuery] string directoryPath, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var logCount = _logProcessor.CountLogsInPeriod(directoryPath, startDate, endDate);
            return Ok(new { totalLogs = logCount });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpGet("search-by-size")]
    public IActionResult SearchLogsBySize([FromQuery] string directoryPath, [FromQuery] long minSizeKb, [FromQuery] long maxSizeKb)
    {
        try
        {
            if (minSizeKb < 0 || maxSizeKb < 0 || minSizeKb > maxSizeKb)
                return BadRequest("Invalid size range. Ensure minSizeKb is less than or equal to maxSizeKb, and both are non-negative.");

            directoryPath ??= Path.Combine(_environment.ContentRootPath, "Logs");

            if (!Directory.Exists(directoryPath))
                return NotFound($"The directory {directoryPath} does not exist.");

            var matchingLogs = _logProcessor.SearchLogsBySizeRange(directoryPath, minSizeKb, maxSizeKb);

            return Ok(new { logs = matchingLogs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("search-by-directory")]
    public IActionResult SearchLogsByDirectory([FromQuery] string directoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return BadRequest("Directory path is required.");

            if (!Directory.Exists(directoryPath))
                return NotFound($"The directory {directoryPath} does not exist.");

            var logFiles = _logProcessor.GetLogFiles(directoryPath);

            return Ok(new { logs = logFiles });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

}
