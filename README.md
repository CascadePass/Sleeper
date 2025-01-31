# Introduction

**Sleep Apnea** is a common problem, affecting 1 in 7 people world wide.  The gold standard treatment for sleep apnea (SA) is CPAP and BiPAP, machines that deliver pressurized air to hold the airway open.  It can be difficult to know how to set a PAP machine up, especially what pressures to use.  An increasing number of patients are diagnosed through home tests, who usually do not go on to do a titration study.

# Project Summary

**Sleeper** is a desktop application designed to help people with sleep SA understand their sleep, with the goal of improving it.  Sleeper can import the logs created by a PAP machine and draw charts to visualize what happened overnight and help users see patterns.

# Technology

Sleeper is written in C# and targets .NET Core.  It uses Avalonia for the UI and SQLite for the database.

Additionally Sleeper rests on the shoulders of giants, and owes a great debt to several open source projects:

* **[StagPoint.EuropeanDataFormat.Net](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/)** to read the EDF files that contain the CPAP data.
* **[cpap-lib](https://github.com/EEGKit/cpap-lib)** allows client applications to read, explore, and analyze the data recorded by a CPAP machine.
* **[Scottplot](https://scottplot.net/)** for charting.
* **[Newtonsoft](https://www.newtonsoft.com/json)** for parsing JSON.

There are [unit tests](https://github.com/CascadePass/Sleeper/tree/master/cpaplib_tests), the Sleeper project uses GitHub Actions for continuous integration (CI).

# Supported Machines

Currently the only fully supported CPAP machine is the **ResMed AirSense 10**, however development is currently happening with a **ResMed AirCurve 10** and **AirCurve 11**.  There has been some (limited) testing with an **AirCurve 10 ASV**.

# What's Next?

After more machines and therapy modes are supported, **Sleeper** will gain the ability to use EEG data from a **Muse S** headband device to distinguish between true central apneas vs "clear airway" events, and to score REM/non-REM breathing.

# Contributing Code

We welcome contributions from other developers!  Please create fork, make a branch with your changnes, and start a pull request.  Code supplied by the community will be covered by the MIT license.

# Screenshots

![DailyReportView-Light.jpg](docs%2FScreenshots%2FDailyReportView-Light.jpg)

![DailyReportView-Light.jpg](docs%2FScreenshots%2FTrendsView-Dark.jpg)

[More Screenshots](docs%2FReadme.md)

# License

Sleeper is licensed under the fairly permissive [MIT License](https://github.com/CascadePass/Sleeper/blob/master/LICENSE).
