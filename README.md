# Context

**Sleep Apnea** (SA) is a common problem, affecting 1 in 7 people world wide.  The gold standard treatment for SA is CPAP and BiPAP, machines that deliver pressurized air to hold the throat open.  It can be difficult to know how to set a machine up, especially what pressures to use.  An increasing number of patients are diagnosed through home tests instead of in labs, and have less complete information about their sleep problems.

Modern machines have auto modes that work well enough for most users, so it's becoming common to not go on to do a titration study.  In the lab, a technician would watch the patient sleep and adjust their pressure settings about every 20 minutes, watching the reaction, going through a titration protocol.  This ends with the best pressure settings for a patient.  When the auto mode isn't working and a titration study isn't an option, people use the logs from their machine to find minimum and maximum pressures to let the machine choose from.

# Project Summary

**Sleeper** is designed to help people with SA understand and improve their sleep.  Sleeper can import the logs created by a PAP machine and draw charts to visualize what happened overnight and help users see patterns.  It's designed to help new PAP users answer questions like:

* Should I run my machine in auto (APAP) or manual (CPAP) mode?
* What pressure(s) should I use?
* How many of these breathing problems are what people call "sleep/wake junk?"  People have poor breathing control during N1 stage sleep, the transition between wakefulness and sleep.  These might cause oxygen problems, but can't disrupt your sleep cycle yet at this stage, so being able to filter them out for a better picture of sleep fragmentation is important, but missing from Oscar and Sleep HQ.

# Technology

Sleeper is a desktop application written in C#, targeting .NET Core v9.  It uses **[Avalonia](Avalonia)** for the UI and **[SQLite](https://www.sqlite.org/index.html)** for the database.  There are [unit tests](https://github.com/CascadePass/Sleeper/tree/master/cpaplib_tests), the Sleeper project uses GitHub Actions for continuous integration (CI).

Additionally Sleeper stands on the shoulders of giants, and owes a great debt to several open source projects:

* **[StagPoint.EuropeanDataFormat.Net](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/)** to read and parse the EDF files that contain the CPAP data.
* **[cpap-lib](https://github.com/EEGKit/cpap-lib)** allows client applications to read, explore, and analyze the data recorded by a CPAP machine.
* **[Scottplot](https://scottplot.net/)** for charting.
* **[Newtonsoft](https://www.newtonsoft.com/json)** for parsing JSON.

# Supported PAP Machines

Sleeper is able to read data from the ResMed AirSense and AirCurve series 10 and 11 machines, and has been tested with the following therapy modes:

* CPAP
* APAP
* BiLevel S
* BiLevel V Auto

There has been some (limited) testing with an **AirCurve 10 ASV**.  Sleeper has code for **AVAPS**, but I don't think it's had any testing.  There is no **S/T** capability.  If anybody out there is using S/T mode, I will add support if you can get me the files on your SD card.

# What's Next?

After some bugs are fixed, **Sleeper** will gain the ability to use EEG data from a **[Muse S](https://choosemuse.com/pages/muse-s)** headband device to distinguish between true central apneas vs "clear airway" events, and to score REM/non-REM breathing.  You can see Sleeper's [task list](https://github.com/users/CascadePass/projects/2/views/1), and [discussions](https://github.com/CascadePass/Sleeper/discussions) if you would like to suggest an improvement.  Finally, you can see [progress toward upcoming releases](https://github.com/CascadePass/Sleeper/milestones?with_issues=no).

# Contributing Code

We welcome contributions from other developers!  Please create a fork, make a branch with your changes, and start a pull request.  Code supplied by the community will be covered by the MIT license.

Additionally, you can submit feature requests, ideas and describe your experience as a user in the [discussions](https://github.com/CascadePass/Sleeper/discussions).

# Screenshots

Sleeper has a light mode and a dark mode.  There is a last night view, and a timeline overview.

![DailyReportView-Light.jpg](docs%2FScreenshots%2FDailyReportView-Light.jpg)

![DailyReportView-Light.jpg](docs%2FScreenshots%2FTrendsView-Dark.jpg)

[More Screenshots](docs%2FReadme.md)

# License

Sleeper is licensed under the fairly permissive [MIT License](https://github.com/CascadePass/Sleeper/blob/master/LICENSE).
