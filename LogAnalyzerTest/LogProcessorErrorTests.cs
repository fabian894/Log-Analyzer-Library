using System.IO;
using Xunit;
using LogAnalyzerLibrary;
using System;

namespace LogAnalyzerTest
{
    public class LogProcessorErrorTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly LogProcessor _logProcessor;

        public LogProcessorErrorTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "LogProcessorErrorTests");
            Directory.CreateDirectory(_testDirectory);

            // Sample log files with errors
            File.WriteAllText(Path.Combine(_testDirectory, "log1.log"), "INFO Starting application\nERROR Failed to connect to database\nERROR Failed to connect to database");
            File.WriteAllText(Path.Combine(_testDirectory, "log2.log"), "ERROR Disk space low\nERROR Disk space low\nERROR Disk space low");
            File.WriteAllText(Path.Combine(_testDirectory, "log3.log"), "INFO Shutting down\nERROR Out of memory");

            _logProcessor = new LogProcessor();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
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
    }
}
