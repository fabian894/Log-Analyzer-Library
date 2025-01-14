using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace LogAnalyzerLibrary
{
    public interface ILogProcessor
    {
        //IEnumerable<string> GetLogFiles(string directoryPath, DateTime? startDate = null, DateTime? endDate = null, long? maxSizeInBytes = null);
        IEnumerable<string> GetLogFiles(string directoryPath, DateTime? startDate = null, long? maxSizeInBytes = null);
        Dictionary<string, int> CountErrors(string directoryPath);
        Dictionary<string, int> CountDuplicatedErrors(string directoryPath);
        void DeleteArchivesFromPeriod(string directoryPath, DateTime startDate, DateTime endDate);
        void ArchiveLogsFromPeriod(string directoryPath, DateTime startDate, DateTime endDate);
        Task UploadLogsToServer(string directoryPath, string serverUrl);
        void DeleteLogsFromPeriod(string directoryPath, DateTime startDate, DateTime endDate);
        int CountLogsInPeriod(string directoryPath, DateTime startDate, DateTime endDate);
        IEnumerable<string> SearchLogsBySizeRange(string directoryPath, long minSizeKb, long maxSizeKb);
        IEnumerable<string> GetLogFiles(string directoryPath);
    }
}
