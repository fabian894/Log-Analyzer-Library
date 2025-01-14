using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LogAnalyzerLibrary
{
    public class LogProcessor : ILogProcessor
    {
        public IEnumerable<string> GetLogFiles(string directoryPath, DateTime? startDate = null, long? maxSizeInBytes = null)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var files = Directory.GetFiles(directoryPath, "*.log");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                if (startDate.HasValue && fileInfo.CreationTime < startDate.Value)
                    continue;

                if (maxSizeInBytes.HasValue && fileInfo.Length > maxSizeInBytes.Value)
                    continue;

                yield return file;
            }
        }

        public Dictionary<string, int> CountErrors(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var errorCounts = new Dictionary<string, int>();

            foreach (var file in Directory.GetFiles(directoryPath, "*.log"))
            {
                var lines = File.ReadAllLines(file);

                foreach (var line in lines)
                {
                    if (line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("could not", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!errorCounts.ContainsKey(line))
                            errorCounts[line] = 0;
                        errorCounts[line]++;
                    }
                }
            }

            return errorCounts;
        }

        public Dictionary<string, int> CountUniqueErrors(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var errorCounts = new Dictionary<string, int>();

            foreach (var file in Directory.GetFiles(directoryPath, "*.log"))
            {
                var lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    if (line.Length >= 10 && DateTime.TryParseExact(line.Substring(0, 10), "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        var messageStartIndex = line.IndexOf(" : ");
                        if (messageStartIndex != -1)
                        {
                            var message = line.Substring(messageStartIndex + 3).Trim();

                            if (!errorCounts.ContainsKey(message))
                            {
                                errorCounts[message] = 0;
                            }
                            errorCounts[message]++;
                        }
                    }
                }
            }

            return errorCounts;
        }

        public Dictionary<string, int> CountDuplicatedErrors(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var duplicateCounts = new Dictionary<string, int>();

            foreach (var file in Directory.GetFiles(directoryPath, "*.log"))
            {
                var lines = File.ReadAllLines(file);
                var processedMessages = new Dictionary<string, int>();

                foreach (var line in lines)
                {
                    if (line.Length >= 10 && DateTime.TryParseExact(line.Substring(0, 10), "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        var messageStartIndex = line.IndexOf(" : ");
                        if (messageStartIndex != -1)
                        {
                            var message = line.Substring(messageStartIndex + 3).Trim();

                            if (!processedMessages.ContainsKey(message))
                            {
                                processedMessages[message] = 0;
                            }
                            processedMessages[message]++;
                        }
                    }
                }

                foreach (var entry in processedMessages)
                {
                    if (entry.Value > 1) 
                    {
                        if (!duplicateCounts.ContainsKey(entry.Key))
                        {
                            duplicateCounts[entry.Key] = 0;
                        }
                        duplicateCounts[entry.Key] += entry.Value - 1; 
                    }
                }
            }

            return duplicateCounts;
        }

        public void DeleteArchivesFromPeriod(string directoryPath, DateTime startDate, DateTime endDate)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var zipFiles = Directory.GetFiles(directoryPath, "*.zip", SearchOption.AllDirectories);

            foreach (var zipFile in zipFiles)
            {
                var creationTime = File.GetCreationTime(zipFile);
                if (creationTime >= startDate && creationTime <= endDate)
                {
                    File.Delete(zipFile);
                    Console.WriteLine($"Deleted archive: {zipFile}");
                }
            }
        }

        public void ArchiveLogsFromPeriod(string directoryPath, DateTime startDate, DateTime endDate)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var logFiles = Directory.GetFiles(directoryPath, "*.log", SearchOption.TopDirectoryOnly)
                .Where(file =>
                {
                    var creationTime = File.GetCreationTime(file);
                    return creationTime >= startDate && creationTime <= endDate;
                }).ToList();

            if (!logFiles.Any())
                throw new InvalidOperationException("No log files found in the specified date range.");

            var zipFileName = $"{startDate:dd_MM_yyyy}-{endDate:dd_MM_yyyy}.zip";
            var zipFilePath = Path.Combine(directoryPath, zipFileName);

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var file in logFiles)
                {
                    zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }

            foreach (var file in logFiles)
            {
                File.Delete(file);
                Console.WriteLine($"Deleted log file: {file}");
            }

            Console.WriteLine($"Logs archived to: {zipFilePath}");
        }

        public async Task UploadLogsToServer(string directoryPath, string serverUrl)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var files = Directory.GetFiles(directoryPath, "*.log");
            if (files.Length == 0)
                throw new FileNotFoundException("No log files found in the specified directory.");

            using (var httpClient = new HttpClient()) 
            {
                foreach (var file in files)
                {
                    try
                    {
                        var content = new MultipartFormDataContent();
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(file));
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "file", Path.GetFileName(file));

                        var response = await httpClient.PostAsync(serverUrl, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Failed to upload {file}: {response.ReasonPhrase}");
                        }
                        else
                        {
                            Console.WriteLine($"Uploaded {file} successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading {file}: {ex.Message}");
                    }
                }
            }
        }

        public void DeleteLogsFromPeriod(string directoryPath, DateTime startDate, DateTime endDate)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var logFiles = Directory.GetFiles(directoryPath, "*.log", SearchOption.AllDirectories);

            foreach (var file in logFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file); 
                    if (DateTime.TryParseExact(fileName, "yyyy.MM.dd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        // Check if the file's date falls within the specified range
                        if (fileDate >= startDate && fileDate <= endDate)
                        {
                            File.Delete(file); // Delete the file
                            Console.WriteLine($"Deleted log file: {file}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped file with invalid date format: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                }
            }
        }


        public int CountLogsInPeriod(string directoryPath, DateTime startDate, DateTime endDate)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var files = Directory.GetFiles(directoryPath, "*.log", SearchOption.AllDirectories);

            var count = files.Count(file =>
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParse(fileName, out var fileDate))
                {
                    return fileDate >= startDate && fileDate <= endDate;
                }
                return false;
            });

            return count;
        }

        public IEnumerable<string> SearchLogsBySizeRange(string directoryPath, long minSizeKb, long maxSizeKb)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            if (minSizeKb < 0 || maxSizeKb < 0 || minSizeKb > maxSizeKb)
                throw new ArgumentException("Size range is invalid. Ensure minSizeKb is less than or equal to maxSizeKb, and both are non-negative.");

            var files = Directory.GetFiles(directoryPath, "*.log", SearchOption.AllDirectories);

            return files.Where(file =>
            {
                var fileSizeKb = new FileInfo(file).Length / 1024.0; 
                return fileSizeKb >= minSizeKb && fileSizeKb <= maxSizeKb;
            });
        }

        public IEnumerable<string> GetLogFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");

            var logFiles = Directory.GetFiles(directoryPath, "*.log", SearchOption.AllDirectories);

            return logFiles;
        }

    }
}

