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

        private string _fileName;

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

        public void Init(string fileName) {
            _fileName = fileName;
        }

        public void AppendToFile(T o) {
            var fileContent = ReadFromFile();
            fileContent = fileContent.Append(o);
            WriteToFile(fileContent);
        }

        public IEnumerable<T> ReadFromFile() {
            var container = readObjectFromFile<Container<T>>();

            return container.Elements;
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
            using (FileStream fs = new FileStream(_fileName, FileMode.Create)) {
                BinaryFormatter bf = new BinaryFormatter();

                bf.Serialize(fs, o);
            }
        }

        private T2 readObjectFromFile<T2>() {
            using (FileStream fs = new FileStream(_fileName, FileMode.Open)) {
                BinaryFormatter bf = new BinaryFormatter();

                return (T2)bf.Deserialize(fs);
            }
        }
    }
}
