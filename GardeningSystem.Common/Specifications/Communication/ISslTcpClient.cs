﻿using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Events.Communication;
using GardeningSystem.Common.Specifications.Communication.Base;

namespace GardeningSystem.Common.Specifications.Communication {
    public interface ISslTcpClient : ITcpClientBaseClass {
    }
}
