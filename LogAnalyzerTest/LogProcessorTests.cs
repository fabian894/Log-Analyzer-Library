using System;
using System.IO;
using System.Linq;
using Xunit;
using LogAnalyzerLibrary;

namespace LogAnalyzerTest
{
    public class LogProcessorTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly LogProcessor _logProcessor;

        public LogProcessorTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "LogProcessorTests");
            Directory.CreateDirectory(_testDirectory);

            File.WriteAllText(Path.Combine(_testDirectory, "log1.log"), "Sample log content 1");
            File.WriteAllText(Path.Combine(_testDirectory, "log2.log"), "Sample log content 2");
            File.SetCreationTime(Path.Combine(_testDirectory, "log1.log"), DateTime.Now.AddDays(-2));
            File.SetCreationTime(Path.Combine(_testDirectory, "log2.log"), DateTime.Now);

            _logProcessor = new LogProcessor();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void GetLogFiles_ShouldReturnAllFiles_WhenNoFiltersApplied()
        {
            var result = _logProcessor.GetLogFiles(_testDirectory);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetLogFiles_ShouldFilterByDateRange()
        {
            var startDate = DateTime.Now.AddDays(-1);
            var result = _logProcessor.GetLogFiles(_testDirectory, startDate);

            Assert.NotNull(result);
            Assert.Single(result); 
        }

        [Fact]
        public void GetLogFiles_ShouldFilterByFileSize()
        {
            var result = _logProcessor.GetLogFiles(_testDirectory, maxSizeInBytes: 10);

            Assert.NotNull(result);
            Assert.Empty(result); 
        }

        [Fact]
        public void GetLogFiles_ShouldThrowException_WhenDirectoryDoesNotExist()
        {
            Assert.Throws<DirectoryNotFoundException>(() => _logProcessor.GetLogFiles("NonExistentDirectory"));
        }

        [Fact]
        public void CountErrors_ShouldReturnCorrectCounts()
        {
            var result = _logProcessor.CountErrors(_testDirectory);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(2, result["ERROR Failed to connect to database"]);
            Assert.Equal(3, result["ERROR Disk space low"]);
            Assert.Equal(1, result["ERROR Out of memory"]);
        }

        [Fact]
        public void CountErrors_ShouldThrowException_WhenDirectoryDoesNotExist()
        {
            Assert.Throws<DirectoryNotFoundException>(() => _logProcessor.CountErrors("NonExistentDirectory"));
        }

        // CountUniqueErrors

        [Fact]
        public void CountUniqueErrors_ShouldReturnCorrectCounts()
        {
            var testDirectory = Path.Combine(Path.GetTempPath(), "LogProcessorTests");
            Directory.CreateDirectory(testDirectory);

            var logFile = Path.Combine(testDirectory, "test.log");
            File.WriteAllLines(logFile, new[]
            {
        "19.10.2019 21:16:44 CLIMaincoreInstaller------>Main : Could not load file",
        "19.10.2019 21:18:09 DataAccessLayer------>ExecuteQuery : Authentication failed",
        "19.10.2019 21:18:09 DataAccessLayer------>ExecuteQuery : Authentication failed",
        "19.10.2019 21:37:13 CLIMaincoreInstaller------>CreateApplicationPool : Application Pool already exists"
    });

            var logProcessor = new LogProcessor();

            var result = logProcessor.CountUniqueErrors(testDirectory);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result["Could not load file"]);
            Assert.Equal(2, result["Authentication failed"]);
            Assert.Equal(1, result["Application Pool already exists"]);

            Directory.Delete(testDirectory, true);
        }

        // DEleteArchivesFromPeriod

        [Fact]
        public void DeleteArchivesFromPeriod_ShouldDeleteMatchingArchives()
        {
            var archivePath = Path.Combine(_testDirectory, "archive.zip");
            File.WriteAllBytes(archivePath, new byte[100]); 
            File.SetCreationTime(archivePath, DateTime.Now.AddDays(-5));

            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            _logProcessor.DeleteArchivesFromPeriod(_testDirectory, startDate, endDate);

            Assert.False(File.Exists(archivePath)); 
        }

        [Fact]
        public void DeleteArchivesFromPeriod_ShouldNotDeleteFilesOutsideRange()
        {
            var archivePath = Path.Combine(_testDirectory, "archive.zip");
            File.WriteAllBytes(archivePath, new byte[100]); 
            File.SetCreationTime(archivePath, DateTime.Now.AddDays(-15));

            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            _logProcessor.DeleteArchivesFromPeriod(_testDirectory, startDate, endDate);

            Assert.True(File.Exists(archivePath)); 
        }

        [Fact]
        public void ArchiveLogsFromPeriod_ShouldCreateZipFileAndDeleteLogs()
        {
            var logFilePath = Path.Combine(_testDirectory, "test.log");
            File.WriteAllText(logFilePath, "Test log content");
            File.SetCreationTime(logFilePath, DateTime.Now.AddDays(-5));

            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            _logProcessor.ArchiveLogsFromPeriod(_testDirectory, startDate, endDate);

            var zipFileName = $"{startDate:dd_MM_yyyy}-{endDate:dd_MM_yyyy}.zip";
            var zipFilePath = Path.Combine(_testDirectory, zipFileName);

            Assert.True(File.Exists(zipFilePath)); 
            Assert.False(File.Exists(logFilePath)); 
        }

        [Fact]
        public void ArchiveLogsFromPeriod_ShouldThrowException_WhenNoLogsFound()
        {
            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now;

            Assert.Throws<InvalidOperationException>(() =>
                _logProcessor.ArchiveLogsFromPeriod(_testDirectory, startDate, endDate));
        }

        [Fact]
        public void CountLogsInPeriod_ShouldReturnCorrectCount_BasedOnFileNames()
        {
            var startDate = new DateTime(2020, 2, 1);
            var endDate = new DateTime(2020, 4, 30);

            var result = _logProcessor.CountLogsInPeriod(_testDirectory, startDate, endDate);

            Assert.Equal(6, result); 
        }

        [Fact]
        public void CountLogsInPeriod_ShouldReturnZero_WhenNoFilesMatch()
        {
            var startDate = new DateTime(2019, 1, 1);
            var endDate = new DateTime(2020, 12, 31);

            var result = _logProcessor.CountLogsInPeriod(_testDirectory, startDate, endDate);

            Assert.Equal(0, result); 
        }

        // SearchLogsBySizeRange

        [Fact]
        public void SearchLogsBySizeRange_ShouldReturnCorrectFiles()
        {
            var minSizeKb = 1;
            var maxSizeKb = 4;

            var result = _logProcessor.SearchLogsBySizeRange(_testDirectory, minSizeKb, maxSizeKb);

            Assert.Contains(Path.Combine(_testDirectory, "2020.02.03.log"), result);
            Assert.Contains(Path.Combine(_testDirectory, "2020.02.06.log"), result);
            Assert.DoesNotContain(Path.Combine(_testDirectory, "2020.05.01.log"), result); 
        }

        [Fact]
        public void SearchLogsBySizeRange_ShouldReturnEmpty_WhenNoFilesMatch()
        {
            var minSizeKb = 10;
            var maxSizeKb = 20;

            var result = _logProcessor.SearchLogsBySizeRange(_testDirectory, minSizeKb, maxSizeKb);

            Assert.Empty(result); 
        }

        [Fact]
        public void SearchLogsBySizeRange_ShouldThrowException_ForInvalidDirectory()
        {
            var minSizeKb = 1;
            var maxSizeKb = 4;

            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                _logProcessor.SearchLogsBySizeRange(@"C:\InvalidPath", minSizeKb, maxSizeKb);
            });
        }

        [Fact]
        public void SearchLogsBySizeRange_ShouldThrowException_ForInvalidSizeRange()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _logProcessor.SearchLogsBySizeRange(_testDirectory, 5, 2); 
            });
        }

        [Fact]
        public void GetLogFiles_ValidDirectoryWithLogFiles_ReturnsLogFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var logFile1 = Path.Combine(tempDir, "log1.log");
            var logFile2 = Path.Combine(tempDir, "log2.log");
            File.WriteAllText(logFile1, "Test Log 1");
            File.WriteAllText(logFile2, "Test Log 2");

            var result = _logProcessor.GetLogFiles(tempDir);

            Assert.Contains(logFile1, result);
            Assert.Contains(logFile2, result);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void GetLogFiles_ValidDirectoryWithoutLogFiles_ReturnsEmpty()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var result = _logProcessor.GetLogFiles(tempDir);

            Assert.Empty(result);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void GetLogFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var exception = Assert.Throws<DirectoryNotFoundException>(() =>
                _logProcessor.GetLogFiles(nonExistentDir));
            Assert.Contains(nonExistentDir, exception.Message);
        }

        [Fact]
        public void GetLogFiles_SubdirectoriesWithLogFiles_ReturnsAllLogFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var subDir = Path.Combine(tempDir, "SubDir");
            Directory.CreateDirectory(subDir);

            var logFile1 = Path.Combine(tempDir, "log1.log");
            var logFile2 = Path.Combine(subDir, "log2.log");
            File.WriteAllText(logFile1, "Test Log 1");
            File.WriteAllText(logFile2, "Test Log 2");

            var result = _logProcessor.GetLogFiles(tempDir);

            Assert.Contains(logFile1, result);
            Assert.Contains(logFile2, result);

            Directory.Delete(tempDir, true);
        }

    }
}
