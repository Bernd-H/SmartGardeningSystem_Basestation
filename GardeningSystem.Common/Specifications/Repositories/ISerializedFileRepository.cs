using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface ISerializedFileRepository<T> where T : IDO {

        void Init(string fileName);

        #region Serialize object lists

        void AppendToFileList(T o);

        IEnumerable<T> ReadListFromFile();

        void WriteListToFile(IEnumerable<T> objects);

        /// <param name="Id">Item id</param>
        /// <returns>True when one or more items got removed.</returns>
        bool RemoveItemFromFileList(Guid Id);

        /// <param name="itemToUpdate">Updated item</param>
        /// <returns>False if not found, otherwise true.</returns>
        bool UpdateItemFromList(T itemToUpdate);

        #endregion


        public void WriteSingleObjectToFile<T2>(T2 o);

        public T2 ReadSingleObjectFromFile<T2>() where T2 : class;
    }
}
