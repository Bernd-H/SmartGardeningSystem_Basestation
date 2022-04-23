This repository contains the source code of the basestation - the software of the gardening system which contains the irrigation algorithm and sends commands to the sensor / valve modules - that was made using .NET Core. (for a project overview see [SmartGardeningSystem](https://github.com/Bernd-H/DA_SmartGardeningSystem))

# Basestation
The basestation contains the following <b>features</b>:
- API & CommandServer for the [mobile app](https://github.com/Bernd-H/DA_SmartGardeningSystem_MobileApp)
- Automatic irrigation service
- Interval service collecting and storing measurements from the sensors
- Services that allow access to the basestation through the internet directly or over the [external server](https://github.com/Bernd-H/DA_GardeningSystem_ExternalServer)
- Service that starts an access point when the raspberry pi is not connected to an wlan

## Usage
The software is developed to run on linux using the .NET 5 Runtime. It can also run on windows when the "accessPointJob_enabled" property of the <b>Configuration/settings.json</b> file is set to false. Just download the latest release and run the <b>GardeningSystem.exe</b> or the <b>GardeningSystem.dll</b> using dotnet core (<i>sudo dotnet GardeningSystem.dll</i>).

## License
This project is licensed under the GPLv3 license.

## Contact
github.smartgardeningsystem@gmail.com
