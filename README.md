# Summary

**Sleeper** is a cross platform desktop application designed to help people with sleep apnea understand their sleep, with the goal of improving it.

# Shoulders of Giants

**Sleeper** is built on **[cpap-lib](https://github.com/EEGKit/cpap-lib)**, a managed (C#/.NET) library that allows client applications to read, explore, and analyze the data recorded by a CPAP machine.

cpap-lib and Sleeper use **[StagPoint.EuropeanDataFormat.Net](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/)** to read the EDF files that contain the CPAP data.

# Supported Machines

Currently the only fully supported CPAP machine is the **ResMed AirSense 10**, however development is currently happening with a **ResMed AirCurve 10** and **AirCurve 11**.  There has been some (limited) testing with an **AirCurve 10 ASV**.

# What's Next?

After more machines and therapy modes are supported, **Sleeper** will gain the ability to use EEG data from a **Muse S** headband device to distinguish between true central apneas vs "clear airway" events, and to score REM/non-REM breathing.

# Screenshots

![DailyReportView-Light.jpg](docs%2FScreenshots%2FDailyReportView-Light.jpg)

[More Screenshots](docs%2FReadme.md)
