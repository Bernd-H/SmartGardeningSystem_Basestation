using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace GardeningSystem.Common.Specifications {
    public interface ILoggerService {
        ILogger GetLogger<T>() where T : class;
    }
}
