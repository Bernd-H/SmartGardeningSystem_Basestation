using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.DataObjects;

namespace GardeningSystem.Common.Models {
    public static class DtoConvertion {
        public static IEnumerable<ModuleInfoDto> ToDtos(this IEnumerable<ModuleInfo> moduleInfos) {
            var objects = new List<ModuleInfoDto>();
            foreach (var module in moduleInfos) {
                objects.Add(ToDto(module));
            }

            return objects;
        }

        public static ModuleInfoDto ToDto(this ModuleInfo module) {
            return new ModuleInfoDto() {
                Id = module.Id,
                AssociatedModules = module.AssociatedModules,
                ModuleTyp = module.ModuleTyp,
                Name = module.Name,
                LastWaterings = module.LastWaterings
            };
        }

        //public static UserDto ToDto(this User user) {
        //    return new UserDto() {
        //        Id = user.Id,
        //        Email = user.Email
        //    };
        //}
    }
}
