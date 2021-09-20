using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class SerializedFileRepository<T> : ISerializedFileRepository<T> where T : IDO {

        private string _filePath;

        private ILogger _logger;

        [Serializable]
        private class Container<T1> where T1 : IDO {
            public IEnumerable<T1> Elements { get; set; }

            public Container() {

            }
        }

        public SerializedFileRepository(ILogger logger) {
            _logger = logger;
        }

        public void Init(string filePath) {
            _filePath = filePath;
        }

        public void AppendToFile(T o) {
            var fileContent = ReadFromFile().ToList();
            fileContent.Add(o);
            WriteToFile(fileContent);
        }

        public IEnumerable<T> ReadFromFile() {
            var container = readObjectFromFile<Container<T>>();

            return container?.Elements ?? new List<T>();
        }

        public void WriteToFile(IEnumerable<T> objects) {
            Container<T> container = new Container<T>();
            container.Elements = objects;

            writeObjectToFile(container);
        }

        public bool RemoveItemFromFile(Guid Id) {
            var fileContent = ReadFromFile().ToList();
            return (fileContent.RemoveAll((o) => o.Id == Id) > 0);
        }

        private void writeObjectToFile<T2>(T2 o) {
            using (FileStream fs = new FileStream(_filePath, FileMode.Create)) {
                BinaryFormatter bf = new BinaryFormatter();

                bf.Serialize(fs, o);
            }
        }

        private T2 readObjectFromFile<T2>() where T2 : class {
            if (File.Exists(_filePath)) {
                using (FileStream fs = new FileStream(_filePath, FileMode.Open)) {
                    BinaryFormatter bf = new BinaryFormatter();

                    return (T2) bf.Deserialize(fs);
                }
            }
            else {
                return null;
            }
        }
    }
}
