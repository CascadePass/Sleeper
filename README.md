<!-- TOC -->
* [Summary](#summary)
* [Dependencies](#dependencies)
* [Binary Distribution](#binary-distribution)
* [Included Data Viewer](#included-data-viewer)
<!-- TOC -->

# Summary

**Sleeper** is a cross platform desktop application designed to help people with sleep apnea understand their sleep, with the goal of improving it.

# Shoulders of Giants

**Sleeper** is built on **cpap-lib**, a managed (C#/.NET) library that allows client applications to read, explore, and analyze the data recorded by a CPAP machine. It can read the CPAP data stored on the SD Card and extract all of the waveform, event, and settings data into an easy to use data model that allows for detailed analysis of everything the CPAP machine is capable of recording. 

Currently the only fully supported CPAP machine is the **ResMed AirSense 10**, because that is the machine I have and therefore the data files I have available to test with.  

I have done some limited testing of the ResMed AirCurve 10 ASV as well, but don't have extensive sample data to test with. Other ResMed Series 10 models may be supported, and Series 11 models may also be at least partially supported. 

There is also very limited "proof of concept" support for the Philips Respironics System One model 560P, but other Philips Respironics models are almost certainly not supported. 

# Dependencies

This library uses [StagPoint.EuropeanDataFormat.Net](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/) to read the EDF files that contain the CPAP data.


![DailyReportView-Light.jpg](docs%2FScreenshots%2FDailyReportView-Light.jpg)

[More Screenshots](docs%2FReadme.md)
