using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models.DTOs {
    public class RfMessageDto : IDO {

        public Guid Id { get; set; }

        public byte[] Bytes { get; set; }

        public RfMessageDto() {

        }
    }
}
