using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Specifications.Repositories {
    public interface ISerializedFileRepository<T> where T : IDO {

        void Init(string fileName);

        void AppendToFile(T o);

        IEnumerable<T> ReadFromFile();

        void WriteToFile(IEnumerable<T> objects);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>True when one or more items got removed.</returns>
        bool RemoveItemFromFile(Guid Id);
    }
}
