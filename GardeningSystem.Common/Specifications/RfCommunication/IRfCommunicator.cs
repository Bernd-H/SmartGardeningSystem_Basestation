using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.RfCommunication {
    public interface IRfCommunicator {

        Task<RfMessageDto> SendMessage_ReceiveAnswer(Guid sender, Guid reciever, byte[] msg);

        Task<RfMessageDto> SendOutBroadcast(byte[] msg);

        void Dispose();
    }
}
