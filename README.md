[![GitHub issues](https://img.shields.io/github/issues/StagPoint/cpap-lib.svg)](https://GitHub.com/StagPoint/cpap-lib/issues/)
[![GitHub license](https://img.shields.io/github/license/StagPoint/cpap-lib.svg)](https://github.com/StagPoint/cpap-lib/blob/master/LICENSE)
![GitHub release (with filter)](https://img.shields.io/github/v/release/StagPoint/cpap-lib)
![GitHub all releases](https://img.shields.io/github/downloads/StagPoint/cpap-lib/total)
[![GitLab last commit](https://badgen.net/github/last-commit/StagPoint/cpap-lib/)](https://github.com/StagPoint/cpap-lib/-/commits)

<!-- TOC -->
* [Summary](#summary)
* [Binary Distribution](#binary-distribution)
* [SD Card Files and Folder Structure](#sd-card-files-and-folder-structure)
    * [Example](#example)
    * [Identification.tgt](#identificationtgt)
    * [STR.edf](#stredf)
    * [DATALOG Folder](#datalog-folder)
<!-- TOC -->

# Summary

**cpap-lib** allows client applications to read and explore CPAP machine data. Currently supports only the **ResMed AirSense 10**, because that is the machine I have and therefore the data files I have available to test with. There is also very limited "proof of concept" support for the Philips Respironics System One model 560P, but other Philips Respironics models are almost certainly not supported. 

# Dependencies 

This library uses [StagPoint.EuropeanDataFormat.Net](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/) to read the EDF files that contain the CPAP data. 

# Included Data Viewer

There is an included Data Viewer application project written in C# and using the Avalonia UI library to display the data imported by cpap-lib.

This viewer application is also open source, and is being actively developed.  

![DailyReportView-Light.jpg](docs%2FScreenshots%2FDailyReportView-Light.jpg)

[More Screenshots](docs%2FReadme.md)