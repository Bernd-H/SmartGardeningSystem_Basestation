using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories
{
    public class FileRepository : IDisposable, IFileRepository
    {
        //private FileStream _fs;
        //private StreamReader _sr;
        //private StreamWriter _sw;

        private ILogger _logger;

        public FileRepository(ILogger logger) {
            _logger = logger;
        }

        public void Dispose() {
            try {
                //_fs?.Close();
            }catch (Exception) {

            }
        }

        public string[] GetContent(string filePath) {
            //openFilestreams(filePath, read: true);
            _logger.Info("Reading from file " + filePath);
            return File.ReadAllLines(filePath);
        }

        public void WriteContent(string filePath, string[] content) {
            //openFilestreams(filePath, read: false);
            _logger.Info("Writing to file " + filePath);
            File.WriteAllLines(filePath, content);
        }

        public void AddLines(string filePath, string[] lines) {
            File.AppendAllLines(filePath, lines);
        }

        //private void openFilestreams(string filePath, bool read = true) {
        //    _fs = new FileStream(filePath, FileMode.OpenOrCreate);
        //    if (read)
        //        _sr = new StreamReader(_fs);
        //    else
        //        _sw = new StreamWriter(_fs);
        //}
    }
}
