using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;

namespace GardeningSystem.Common.Specifications.Managers
{
    public interface IWateringManager
    {
        Task<IEnumerable<WateringNeccessaryDto>> IsWateringNeccessary();

        Task StartWatering(WateringNeccessaryDto wateringInfo);
    }
}
