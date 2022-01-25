using System;
using System.Collections.Generic;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Specifications.Repositories {

    /// <summary>
    /// Class to save/load one or multiple objects in/from a file.
    /// </summary>
    /// <typeparam name="T">Type of the object/objects.</typeparam>
    public interface ISerializedFileRepository<T> where T : IDO {

        /// <summary>
        /// Sets the fileName or path of an file, where the objects get/are stored in.
        /// </summary>
        /// <param name="fileName"></param>
        void Init(string fileName);

        #region Serialize object lists

        /// <summary>
        /// Adds an object to multiple objects stored in a file.
        /// </summary>
        /// <param name="o">Object to add.</param>
        void AppendToFileList(T o);

        /// <summary>
        /// Reads all objects from the file.
        /// </summary>
        /// <returns>List of objects of with type <typeparamref name="T"/>.</returns>
        IEnumerable<T> ReadListFromFile();

        /// <summary>
        /// Writes multiple objects to the file.
        /// Does not append them. Overrides the hole file.
        /// </summary>
        /// <param name="objects"></param>
        void WriteListToFile(IEnumerable<T> objects);

        /// <summary>
        /// Removes a object from the file.
        /// </summary>
        /// <param name="Id">Id of the item to remove.</param>
        /// <returns>True when one or more items got removed.</returns>
        bool RemoveItemFromFileList(Guid Id);

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="itemToUpdate">Updated item</param>
        /// <returns>False if not found, otherwise true.</returns>
        bool UpdateItemFromList(T itemToUpdate);

        #endregion

        /// <summary>
        /// Writes an object to the file.
        /// Overrides the file.
        /// </summary>
        /// <typeparam name="T2">Type of the object to store.</typeparam>
        /// <param name="o">Object that should get stored.</param>
        public void WriteSingleObjectToFile<T2>(T2 o) where T2 : class;

        /// <summary>
        /// Reads an object of the file.
        /// </summary>
        /// <typeparam name="T2">Type of the object to load.</typeparam>
        /// <returns>Deserialized object.</returns>
        public T2 ReadSingleObjectFromFile<T2>() where T2 : class;
    }
}
