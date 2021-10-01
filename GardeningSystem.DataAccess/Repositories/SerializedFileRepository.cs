using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
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

        public void Init(string fileName) {
            _filePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName + "\\" + fileName;
        }

        #region Serilize list of objects

        public void AppendToFileList(T o) {
            var fileContent = ReadListFromFile().ToList();
            fileContent.Add(o);
            WriteListToFile(fileContent);
        }

        public IEnumerable<T> ReadListFromFile() {
            var container = ReadSingleObjectFromFile<Container<T>>();

            return container?.Elements ?? new List<T>();
        }

        public void WriteListToFile(IEnumerable<T> objects) {
            Container<T> container = new Container<T>();
            container.Elements = objects;

            WriteSingleObjectToFile(container);
        }

        public bool RemoveItemFromFileList(Guid Id) {
            var fileContent = ReadListFromFile().ToList();
            bool removed = (fileContent.RemoveAll((o) => o.Id == Id) > 0);
            if (removed) {
                // update file
                WriteListToFile(fileContent);
            }

            return removed;
        }

        public bool UpdateItemFromList(T itemToUpdate) {

            var items = ReadListFromFile().ToList();

            // find current item
            var oldItem = items.Find(i => i.Id == itemToUpdate.Id);
            if (oldItem != null) {
                bool removed = items.Remove(oldItem);
                if (removed) {
                    items.Add(itemToUpdate);

                    // update file
                    WriteListToFile(items);

                    return true;
                }
            }

            return false;
        }

        #endregion

        public void WriteSingleObjectToFile<T2>(T2 o) {
            //using (FileStream fs = new FileStream(_filePath, FileMode.Create)) {
            //    BinaryFormatter bf = new BinaryFormatter();

            //    bf.Serialize(fs, o);
            //}
            File.WriteAllText(_filePath, JsonSerializer.Serialize(o));
        }

        public T2 ReadSingleObjectFromFile<T2>() where T2 : class {
            if (File.Exists(_filePath)) {
                //using (FileStream fs = new FileStream(_filePath, FileMode.Open)) {
                //    BinaryFormatter bf = new BinaryFormatter();

                //    return (T2) bf.Deserialize(fs);
                //}
                return JsonSerializer.Deserialize<T2>(File.ReadAllText(_filePath));
            }
            else {
                return null;
            }
        }
    }
}
