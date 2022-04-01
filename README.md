This repository contains the source code of the basestation - the software of the gardening system which contains the irrigation algorithm and sends commands to the sensor / valve modules - that was made using .NET Core. (for an project overview see [SmartGardeningSystem](https://github.com/Bernd-H/DA_SmartGardeningSystem))

The software is developed to run on linux using the .NET 5 Runtime. It can also run on windows when the "accessPointJob_enabled" property of the Configuration/settings.json file is set to false. 

# Basestation
The basestation contains the following features:
- API & CommandServer for the [mobile app](https://github.com/Bernd-H/DA_SmartGardeningSystem_MobileApp)
- Automatic irrigation service
- Interval service collecting and storing measurements from the sensors
- Services that allow access to the basestation through the internet directly or over the [external server](https://github.com/Bernd-H/DA_GardeningSystem_ExternalServer)
- Service that starts an access point when the raspberry pi is not connect to an wlan

# License
This project is licensed under the GPLv3 license.

# Contact
