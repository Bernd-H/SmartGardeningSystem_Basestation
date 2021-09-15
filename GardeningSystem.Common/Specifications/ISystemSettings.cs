using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications
{
    interface ISystemSettings
    {
        DateTime LastWateringTime { get; set; }
    }
}
