using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.DataObjects;
using GardeningSystem.Common.Specifications.Repositories;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {

    /// <inheritdoc/>
    public class SerializedFileRepository<T> : ISerializedFileRepository<T> where T : IDO {

        private static object OBJECT_LOCKER = new object();

        private static object LIST_LOCKER = new object();


        private string _filePath;

        private ILogger _logger;

        [Serializable]
        private class Container<T1> where T1 : IDO {
            public IEnumerable<T1> Elements { get; set; }

            public Container() {

            }
        }

        public SerializedFileRepository(ILoggerService logger) {
            _logger = logger.GetLogger<SerializedFileRepository<T>>();
        }

        /// <inheritdoc/>
        public void Init(string fileName) {
            _filePath = ConfigurationContainer.GetFullPath(fileName);
            _logger.Trace($"[Init]Repository filepath set to {_filePath}.");
        }

        #region Serilize list of objects

        /// <inheritdoc/>
        public void AppendToFileList(T o) {
            lock (LIST_LOCKER) {
                _logger.Trace($"[AppendToFileList]Appending object with id={o.Id} to list at {new FileInfo(_filePath).Name}.");
                var fileContent = readListFromFile().ToList();
                fileContent.Add(o);
                writeListToFile(fileContent);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<T> ReadListFromFile() {
            lock (LIST_LOCKER) {
                return readListFromFile();
            }
        }

        /// <inheritdoc/>
        public void WriteListToFile(IEnumerable<T> objects) {
            lock(LIST_LOCKER) {
                writeListToFile(objects);
            }
        }

        /// <inheritdoc/>
        public bool RemoveItemFromFileList(Guid Id) {
            lock (LIST_LOCKER) {
                _logger.Trace($"[RemoveItemFromFileList]Removing object with id={Id} from file {new FileInfo(_filePath).Name}.");
                var fileContent = readListFromFile().ToList();
                bool removed = (fileContent.RemoveAll((o) => o.Id == Id) > 0);
                if (removed) {
                    // update file
                    writeListToFile(fileContent);
                }

                return removed;
            }
        }

        /// <inheritdoc/>
        public bool UpdateItemFromList(T itemToUpdate) {
            lock (LIST_LOCKER) {
                _logger.Trace($"[UpdateItemFromList]Updating object with id={itemToUpdate.Id} from file {new FileInfo(_filePath).Name}.");
                var items = readListFromFile().ToList();

                // find current item
                var oldItem = items.Find(i => i.Id == itemToUpdate.Id);
                if (oldItem != null) {
                    bool removed = items.Remove(oldItem);
                    if (removed) {
                        items.Add(itemToUpdate);

                        // update file
                        writeListToFile(items);

                        return true;
                    }
                }
                else {
                    _logger.Error($"[UpdateItemFromList]Object not found.");
                }

                return false;
            }
        }

        private void writeListToFile(IEnumerable<T> objects) {
            _logger.Trace($"[WriteListToFile]Writing list of objects to {new FileInfo(_filePath).Name}.");
            Container<T> container = new Container<T>();
            container.Elements = objects;

            WriteSingleObjectToFile(container);
        }

        private IEnumerable<T> readListFromFile() {
            _logger.Trace($"[ReadListFromFile]Reading list form file {new FileInfo(_filePath).Name}.");
            var container = ReadSingleObjectFromFile<Container<T>>();

            return container?.Elements ?? new List<T>();
        }

        #endregion

        /// <inheritdoc/>
        public void WriteSingleObjectToFile<T2>(T2 o) where T2 : class {
            lock (OBJECT_LOCKER) {
                _logger.Trace($"[WriteSingleObjectToFile]Writing object to file {new FileInfo(_filePath).Name}.");
                File.WriteAllText(_filePath, JsonSerializer.Serialize(o));
            }
        }

        /// <inheritdoc/>
        public T2 ReadSingleObjectFromFile<T2>() where T2 : class {
            lock (OBJECT_LOCKER) {
                if (File.Exists(_filePath)) {
                    _logger.Trace($"[ReadSingleObjectFromFile]Reading object from file {new FileInfo(_filePath).Name}.");
                    return JsonSerializer.Deserialize<T2>(File.ReadAllText(_filePath));
                }
                else {
                    _logger.Trace($"[ReadSingleObjectFromFile]File {_filePath} does not exist.");
                    return null;
                }
            }
        }
    }
}
