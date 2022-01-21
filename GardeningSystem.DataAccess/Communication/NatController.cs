using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;
using Mono.Nat;
using NLog;

namespace GardeningSystem.DataAccess.Communication {

    /// <inheritdoc/>
    public class NatController : INatController {

        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        private List<INatDevice> natDevices = new List<INatDevice>();

        private List<Mapping> performedMappings = new List<Mapping>();


        private ILogger Logger;

        public NatController(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<NatController>();
        }

        /// <inheritdoc/>
        public void StartSearchingForNatDevices() {
            Logger.Info($"[StartSearchingForNatDevices]Searching for nats.");
            NatUtility.DeviceFound += deviceFound;
            NatUtility.StartDiscovery();
        }

        /// <inheritdoc/>
        public async Task CloseAllOpendPorts() {
            await locker.WaitAsync();
            try {
                Logger.Info($"[CloseAllOpendPorts]Closing {performedMappings.Count} open ports.");
                foreach (var mapping in performedMappings) {
                    await closeMapping(mapping);
                }

                performedMappings.Clear();
            }
            finally {
                locker.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ClosePublicPort(int publicPort, bool tcp = true) {
            await locker.WaitAsync();
            try {
                Protocol protocol = tcp ? Protocol.Tcp : Protocol.Udp;

                var mapping = performedMappings.Find((m) => {
                    return (m.Protocol == protocol) && (m.PublicPort == publicPort);
                });

                await closeMapping(mapping);

                performedMappings.Remove(mapping);
            }
            finally {
                locker.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<int> OpenPublicPort(int privatePort, int publicPort, bool tcp = true) {
            await locker.WaitAsync();
            Protocol protocol = tcp ? Protocol.Tcp : Protocol.Udp;

            try {
                Dictionary<IPAddress, INatDevice> devicesWithNewMapping = new Dictionary<IPAddress, INatDevice>();
                var mapping = new Mapping(protocol, privatePort, publicPort);
                bool mappingFailed = false;

                foreach (var device in natDevices) {
                    // natDevices can contain one device multiple times just with another nat protocol
                    // so skip devices where the mapping was already successful
                    var externalIP = await device.GetExternalIPAsync();
                    if (!devicesWithNewMapping.ContainsKey(externalIP)) {
                        var actualMapping = mapping;

                        try {
                            // add new mapping
                            actualMapping = await device.CreatePortMapAsync(mapping);
                            Logger.Info("[OpenPublicPort]Create mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);

                            // confirm mapping
                            //try {
                            Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, actualMapping.PublicPort);
                            Logger.Info("[OpenPublicPort]Confirmed mapping: protocol={0}, public={1}, private={2}", m.Protocol, m.PublicPort, m.PrivatePort);
                            devicesWithNewMapping.Add(externalIP, device);
                        }
                        catch {
                            Logger.Info("[OpenPublicPort]Couldn't get specific mapping");
                            mappingFailed = true;
                            break;
                        }

                        // obsolete since device.CreatePortMapAsync does throw an exception when the public port is not available...
                        if (actualMapping != mapping && devicesWithNewMapping.Count > 1) {
                            // public port is already in use on this nat
                            // delete made entries on the the other devices and map another public port instead
                            // target: all found nat devices have mapped the same public port
                            mapping = actualMapping;
                            await processMappingFailed(devicesWithNewMapping, mapping);
                            Logger.Trace($"[OpenPublicPort]Retrying to map public port: {mapping.PublicPort} instead of {mapping.PrivatePort}.");
                            return await OpenPublicPort(privatePort, mapping.PublicPort, tcp);
                        }
                        else if (actualMapping != mapping && devicesWithNewMapping.Count == 1) {
                            mapping = actualMapping;
                        }
                    }
                }

                if (mappingFailed || natDevices.Count == 0) {
                    await processMappingFailed(devicesWithNewMapping, mapping);
                    return -1;
                }
                else {
                    performedMappings.Add(mapping);
                    return mapping.PublicPort;
                }
            }
            finally {
                locker.Release();
            }
        }

        private async Task processMappingFailed(Dictionary<IPAddress, INatDevice> devicesWithNewMapping, Mapping mapping) {
            // delete mapping on the other devices
            foreach (var device in devicesWithNewMapping) {
                // try deleting the port we opened before
                try {
                    await device.Value.DeletePortMapAsync(mapping);
                    Logger.Info("[processMappingFailed]Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
                }
                catch {
                    Logger.Warn("[processMappingFailed]Couldn't delete specific mapping");
                }
            }
        }

        private async Task closeMapping(Mapping mapping) {
            List<IPAddress> devicesWithRemovedMapping = new List<IPAddress>();

            if (mapping != null) {
                // delete mapping on all nat devices
                foreach (var device in natDevices) {
                    var externalDeviceIp = await device.GetExternalIPAsync();
                    if (!devicesWithRemovedMapping.Contains(externalDeviceIp)) {
                        try {
                            await device.DeletePortMapAsync(mapping);
                            Logger.Info("[ClosePublicPort]Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
                            devicesWithRemovedMapping.Add(externalDeviceIp);
                        }
                        catch {
                            Logger.Info("[ClosePublicPort]Couldn't delete specific mapping");
                        }
                    }
                }

                //performedMappings.Remove(mapping);
            }
        }

        private async void deviceFound(object sender, DeviceEventArgs args) {
            await locker.WaitAsync();
            try {
                INatDevice device = args.Device;

                // Only interact with one device at a time. Some devices support both
                // upnp and nat-pmp.

                Logger.Info($"[DeviceFound]{device.NatProtocol} supporting nat with ip={await device.GetExternalIPAsync()} found.");
                if (!natDevices.Contains(device)) {
                    natDevices.Add(device);
                }
            }
            finally {
                locker.Release();
            }
        }
    }
}
