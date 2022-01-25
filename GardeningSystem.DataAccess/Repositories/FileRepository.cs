using System;
using System.IO;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {


    public class FileRepository : IFileRepository {

        private ILogger Logger;

        public FileRepository(ILoggerService logger) {
            Logger = logger.GetLogger<FileRepository>();
        }

        public string[] GetContent(string filePath) {
            Logger.Trace($"[GetContent]Reading from file {filePath}.");
            return File.ReadAllLines(filePath);
        }

        public void WriteContent(string filePath, string[] content) {
            Logger.Trace($"[WriteContent]Writing to file {filePath}.");
            File.WriteAllLines(filePath, content);
        }

        public void AddLines(string filePath, string[] lines) {
            Logger.Trace($"[AddLines]Appending to file {filePath} {lines.Length} lines.");
            File.AppendAllLines(filePath, lines);
        }
    }
}
