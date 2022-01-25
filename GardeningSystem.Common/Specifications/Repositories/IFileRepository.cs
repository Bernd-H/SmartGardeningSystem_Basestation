using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Repositories {

    /// <summary>
    /// 
    /// </summary>
    public interface IFileRepository {

        string[] GetContent(string filePath);

        void WriteContent(string filePath, string[] content);

        public void AddLines(string filePath, string[] lines);
    }
}
