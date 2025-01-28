using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;


#if IMPORT_ASYNC
using System.Threading.Tasks;
#endif

using StagPoint.EDF.Net;

// ReSharper disable MergeIntoLogicalPattern
// ReSharper disable CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable ConvertToUsingDeclaration

namespace cpaplib
{
    public class ResMedDataLoader : ICpapDataLoader
    {
		#region Private fields

        /// <summary>
        /// The list of known Model Numbers for the ResMed CPAP Line from 10 and up
        /// </summary>
        private static Dictionary<string, string> ModelNumbers = new Dictionary<string, string>()
        {
            { "37201", "ResMed AirStart 10" },
            { "37202", "ResMed AirStart 10" },
            { "37203", "ResMed AirSense 10" },
            { "37028", "ResMed AirSense 10 AutoSet" },
            { "37209", "ResMed AirSense 10 AutoSet For Her" },
            { "37382", "ResMed AirSense 10 AutoSet (Card to Cloud)" },
            { "37205", "ResMed AirSense 10 Elite" },
            { "37213", "ResMed AirCurve 10 S BiLevel" },
            { "37211", "ResMed AirCurve 10 VAuto BiLevel" },
            { "37383", "ResMed AirCurve 10 VAuto BiLevel (Card to Cloud)" },
            { "39000", "ResMed AirSense 11 AutoSet" },
        };

        private static string[] expectedFiles = new[]
        {
            "STR.edf",
        };

        private static string[] expectedFolders = new[]
        {
            // "SETTINGS",
            "DATALOG",
        };

        private static Dictionary<string, int> identificationFileNames = new()
        {
            { "Identification.tgt", 10 },
            { "Identification.json", 11 },
        };

        private static Dictionary<int, OperatingMode> s_modeMapping = new Dictionary<int, OperatingMode>()
        {
            { 11, OperatingMode.Apap }, // "For Her" model, which is just AutoCPAP with custom algorithms
            { 9, OperatingMode.Avaps },
            { 8, OperatingMode.AsvVariableEpap },
            { 7, OperatingMode.Asv },
            { 6, OperatingMode.BilevelAutoFixedPS },
            { 5, OperatingMode.BilevelFixed },
            { 4, OperatingMode.BilevelFixed },
            { 3, OperatingMode.BilevelFixed },
            { 2, OperatingMode.BilevelFixed },
            { 1, OperatingMode.Apap },
            { 0, OperatingMode.Cpap },
        };

        private MachineIdentification _machineInfo    = new MachineIdentification();
        private CpapImportSettings    _importSettings = new CpapImportSettings();

        #endregion

        #region Public API

        public bool HasCorrectFolderStructure(string rootFolder)
        {
            return this.HasCorrectFolderStructure(rootFolder, false);
        }

        public bool HasCorrectFolderStructure( string rootFolder, bool throwIfNot )
        {
            foreach( var folder in expectedFolders )
            {
                var directoryPath = Path.Combine( rootFolder, folder );
                if( !Directory.Exists( directoryPath ) )
                {
                    if (throwIfNot)
                    {
                        throw new DirectoryNotFoundException($"Directory {directoryPath} does not exist");
                    }

                    return false;
                }
            }

            foreach( var filename in expectedFiles )
            {
                var filePath = Path.Combine( rootFolder, filename );
                if( !File.Exists( filePath ) )
                {
                    if (throwIfNot)
                    {
                        throw new FileNotFoundException($"File {filePath} does not exist");
                    }

                    return false;
                }
            }

            foreach (var filename in identificationFileNames.Keys)
            {
                var filePath = Path.Combine(rootFolder, filename);
                if (File.Exists(filePath))
                {
                    return true;
                }
            }

            if (throwIfNot)
            {
                throw new FileNotFoundException($"Device Identificaiton File does not exist");
            }

            return true;
        }

        #region Get Machine Identification

        public MachineIdentification LoadMachineIdentificationInfo(string rootFolder)
        {
            foreach (var item in identificationFileNames)
            {
                var filename = item.Key;
                var filePath = Path.Combine(rootFolder, filename);

                if (File.Exists(filePath))
                {
                    var machineVersion = item.Value;

                    switch (machineVersion)
                    {
                        case 10:
                            return LoadMachineIdentificationInfo_ResMedSeries10(rootFolder);
                        case 11:
                            return LoadMachineIdentificationInfo_ResMedSeries11(rootFolder);
                    }
                }
            }

            throw new FileNotFoundException("Device Identification File does not exist");
        }

        public MachineIdentification LoadMachineIdentificationInfo_ResMedSeries10( string rootFolder )
        {
            var filename    = Path.Combine( rootFolder, "Identification.tgt" );

            if (!File.Exists(filename))
            {
                return null;
            }

            using( var file = File.OpenRead( filename ) )
            {
                return this.GetMachineIdentificationFromStream(file);
            }
        }

        public MachineIdentification LoadMachineIdentificationInfo_ResMedSeries11(string rootFolder)
        {
            var filename = Path.Combine(rootFolder, "Identification.json");

            if (File.Exists(filename))
            {
                var rawJsonData = File.ReadAllText(filename);

                return this.GetMachineIdentificationFromJson(rawJsonData);
            }

            return null;
        }

        #region Parsle ID File

        public MachineIdentification GetMachineIdentificationFromJson(string json)
        {
            var result = JsonConvert.DeserializeObject<JsonMachineIdentificationFile>(json);

            var machineInfo = new MachineIdentification
            {
                Manufacturer = MachineManufacturer.ResMed,
                ProductName = result?.FlowGenerator?.IdentificationProfiles?.Product?.ProductName,
                SerialNumber = result?.FlowGenerator?.IdentificationProfiles?.Product?.SerialNumber,
                ModelNumber = result?.FlowGenerator?.IdentificationProfiles?.Product?.ProductCode,
            };

            return machineInfo;
        }

        public MachineIdentification GetMachineIdentificationFromStream(Stream stream)
        {
            var machineInfo = new MachineIdentification();
            var fields = new Dictionary<string, string>();

            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int spaceIndex = line.IndexOf(" ", StringComparison.Ordinal);
                Debug.Assert(spaceIndex != -1);

                var key = line.Substring(1, spaceIndex - 1);
                var value = line.Substring(spaceIndex + 1).Trim().Replace('_', ' ');

                fields[key] = value;
            }

            machineInfo.Manufacturer = MachineManufacturer.ResMed;
            machineInfo.ProductName = fields["PNA"];
            machineInfo.SerialNumber = fields["SRN"];
            machineInfo.ModelNumber = fields["PCD"];

            return machineInfo;
        }

        #endregion

        #endregion

        public List<DailyReport> LoadFromFolder( string rootFolder, DateTime? minDate = null, DateTime? maxDate = null, CpapImportSettings importSettings = null )
        {
            _importSettings = importSettings ?? new CpapImportSettings();

            EnsureCorrectFolderStructure( rootFolder );

            _machineInfo = LoadMachineIdentificationInfo( rootFolder );

            var indexFilename = Path.Combine( rootFolder, "STR.edf" );
            var days          = LoadIndexAndSettings( indexFilename, minDate ?? DateTime.MinValue, maxDate ?? DateTime.MaxValue );

#if IMPORT_ASYNC
			var tasks = new Task[ days.Count ];

			for( int i = 0; i < days.Count; i++ )
			{
				var day = days[ i ];

				tasks[ i ] = Task.Run( () =>
				{
					// Loads all event and session data for the given day
					ImportSessionsAndEvents( rootFolder, day );
				} );
			}

			Task.WaitAll( tasks );
#else
            foreach( var day in days )
            {
                // Loads all event and session data for the given day
                ImportSessionsAndEvents( rootFolder, day );
            }
#endif

            // Remove all days that don't have any Sessions. This might happen, for instance, when all Sessions were 
            // shorter than the ImportSessions.MinimumSessionLength value (particularly when there is a single short 
            // Session that occurred after the "ResMed Noon day cutoff")
            days.RemoveAll( x => x.Sessions.Count == 0 );

            // Make sure that each Session has its Source set. Sessions may be created by other processes, such as
            // pulse oximeter import, etc., and this will help to differentiate them. 
            foreach( var day in days )
            {
                Debug.Assert( !day.Sessions.Any( x => Math.Ceiling( x.Duration.TotalMinutes ) < _importSettings.MinimumSessionLength ) );

                foreach( var session in day.Sessions )
                {
                    session.Source = _machineInfo.ProductName;
                }
            }

            // Fix up the ReportDate field to eliminate the stupid 12 hour time offset added by the ResMed machine. 
            // It was important (or at least conveniently useful) to retain it during some phases of the import process,
            // but now import is done and we don't need that offset anymore.
            foreach( var day in days )
            {
                day.ReportDate = day.ReportDate.Date;
            }

            return days;
        }

		#endregion

		#region Private functions

        private void EnsureCorrectFolderStructure( string rootFolder )
        {
            this.HasCorrectFolderStructure(rootFolder, true);
        }

        private void ImportSessionsAndEvents( string rootFolder, DailyReport day )
        {
            var logFolder = Path.Combine( rootFolder, $@"DATALOG\{day.ReportDate:yyyyMMdd}" );
            if( !Directory.Exists( logFolder ) )
            {
                // TODO: How to better handle dates where the SD Card wasn't inserted?

                // The only reason I've seen so far for the session directory to not exist for a given day is when
                // the user has forgotten to put the SD Card back into the machine. On such days, you will still 
                // have summary information available (such as settings, AHI, mask times, etc), but no graph data
                // will be found. 
                // 
                // For now, these dates are not even imported. This needs to get fixed. 

                Debug.WriteLine( $"Could not find the session directory for {logFolder}. Likely missing SD Card storage for this day." );

                return;
            }

            LoadEventsAndAnnotations( logFolder, day );
            LoadCheyneStokesEvents( logFolder, day );

            var filenames = Directory.GetFiles( logFolder, "*.edf" );

            // I believe the filenames are already sorted, but since it's important that they are sorted by date
            // and time, we'll explicitly sort them. Note that this coincidentally sorts the high-resolution 
            // signals before the low-resolution signals, which is a nice bonus.
            Array.Sort( filenames );

            foreach( var filename in filenames )
            {
                var baseFilename = Path.GetFileNameWithoutExtension( filename );

                // EVE and CSL files are handled separately
                var ignoreThisFile =
                    baseFilename.EndsWith( "_CSL", StringComparison.OrdinalIgnoreCase ) ||
                    baseFilename.EndsWith( "_EVE", StringComparison.OrdinalIgnoreCase );
                if( ignoreThisFile )
                {
                    continue;
                }

                // The file's date, extracted from the filename, will be used to search for the correct session.
                // The date/time is incorrect for any other purpose (I think).
                // NOTE: The time adjustment needs to be added to the file timestamp to match the already-modified session timestamp
                var fileDate = DateTime
                    .ParseExact( baseFilename.Substring( 0, baseFilename.Length - 4 ), "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture )
                    .Trim( TimeSpan.TicksPerMinute )
                    .AdjustImportTime( _importSettings );

                int sessionIndex = 0;
                while( sessionIndex < day.Sessions.Count )
                {
                    var session = day.Sessions[ sessionIndex++ ];

                    // If the file's extracted date does not intersect with the session, then it's not relevant to that session 
                    // NOTE: It's possible that the file time is actually off by about a minute, so a one minute fudge factor is added.
                    if( session.StartTime > fileDate.AddMinutes( 1 ) || session.EndTime < fileDate )
                    {
                        continue;
                    }

                    using( var file = File.OpenRead( filename ) )
                    {
                        // Sigh. There are some EDF headers that have invalid values, and we need to check that first.
                        var header = new EdfFileHeader( file );
                        if( header.NumberOfDataRecords <= 0 )
                        {
                            break;
                        }

                        // Now rewind the file, because there's currently no way to "continue reading the file from here"
                        file.Position = 0;

                        // Read in the EDF file 
                        var edf = new EdfFile();
                        edf.ReadFrom( file );

                        foreach( var signal in edf.Signals )
                        {
                            var signalName = SignalNames.GetStandardName( signal.Label.Value );

                            // We don't need to keep the CRC checksum signals 
                            if( signalName.StartsWith( "crc", StringComparison.InvariantCultureIgnoreCase ) )
                            {
                                continue;
                            }

                            if( signalName.Equals( SignalNames.MaskPressureLow, StringComparison.Ordinal ) )
                            {
                                // If we already have high-resolution Mask Pressure signal data, ignore the low-resolution 
                                if( session.Signals.Any( x => x.Name == SignalNames.MaskPressure ) )
                                {
                                    continue;
                                }

                                // Otherwise, rename the low-resolution mask pressure data for consistency 
                                signalName = signal.Label.Value = SignalNames.MaskPressure;
                            }

                            // Some signals should be scaled to match the other signals
                            var scaleToLitersPerMinute = signalName.Equals( SignalNames.FlowRate, StringComparison.Ordinal ) ||
                                signalName.Equals( SignalNames.LeakRate,                          StringComparison.Ordinal );
                            if( scaleToLitersPerMinute )
                            {
                                // Convert from L/s to L/m
                                signal.PhysicalDimension.Value =  "L/min";
                                signal.PhysicalMaximum.Value   *= 60;
                                signal.PhysicalMinimum.Value   *= 60;

                                for( int i = 0; i < signal.Samples.Count; i++ )
                                {
                                    signal.Samples[ i ] *= 60;
                                }
                            }
                            else if( signalName.Equals( SignalNames.TidalVolume, StringComparison.Ordinal ) )
                            {
                                // I don't know why ResMed reports Tidal Volume in Liters, but ml is the standard
                                signal.PhysicalDimension.Value =  "ml";
                                signal.PhysicalMaximum.Value   *= 1000;
                                signal.PhysicalMinimum.Value   *= 1000;

                                for( int i = 0; i < signal.Samples.Count; i++ )
                                {
                                    signal.Samples[ i ] *= 1000;
                                }
                            }

                            // Not every signal within a Session will have the same start and end time as the others
                            // because of differences in sampling rate, so we keep track of the start time and end
                            // time of each Signal separately. Note the addition of a time adjustment, which allows
                            // the user to calibrate for "drift" of the ResMed machine's internal clock.  
                            var startTime = header.StartTime.Value.AdjustImportTime( _importSettings );
                            var endTime   = startTime.AddSeconds( header.NumberOfDataRecords * header.DurationOfDataRecord );

                            // Discard Signals that are shorter than the minimum Session duration. This is done as an indirect
                            // method of discarding Sessions that are shorter than the specified minimum duration, because 
                            // Session duration is tied to the duration of the Signals it contains. It is highly unlikely (as
                            // in never seen in actual practice so far) that signals within a single Session will be different
                            // enough in duration that some signals for that Session fall under the minimum while some are over
                            // the minimum, but it is technically possible. Rounding the Signal's duration to the next highest
                            // whole number of minutes should effectively eliminate that possibility in actual practice. 
                            if( Math.Ceiling( (endTime - startTime).TotalMinutes ) < _importSettings.MinimumSessionLength )
                            {
                                continue;
                            }

                            // We need to see if the session already contains a signal by this name, so we know what to do with it. 
                            if( session.GetSignalByName( signalName ) == null )
                            {
                                // Add the signal to the current session
                                AddSignalToSession( session, startTime, endTime, signal );
                            }
                            else
                            {
                                // If a session already contains a signal by this name, it means that the ResMed machine has
                                // given us MaskOn/MaskOff times that do not match the stored files. This happens when there
                                // is a brief interruption in recording such as turning the machine off then back on again 
                                // within a short enough time period that it doesn't start a new session, which ends up creating
                                // a discontinuous session.
                                //
                                // We don't allow discontinuous sessions, so when this occurs the simplest thing to do is split
                                // the session by creating another one with the same start and end times and adding the signal to
                                // that session instead. We don't need to worry about the overlapping times because those will
                                // be trimmed to match the stored signals below.

                                // Check to see if there is an in-progress session that does not yet have this signal.
                                var splitSession = day.Sessions.FirstOrDefault( x => session.StartTime <= fileDate && session.EndTime >= fileDate && x.GetSignalByName( signalName ) == null );

                                // If there is, add it. 
                                if( splitSession != null )
                                {
                                    AddSignalToSession( splitSession, startTime, endTime, signal );
                                }
                                else
                                {
                                    // Create a new Session based on the current session's time period, which will contain all of
                                    // the discontinuous signals (or at least the next set, as there may be more than one).
                                    var newSession = new Session()
                                    {
                                        StartTime = session.StartTime,
                                        EndTime   = session.EndTime,
                                    };

                                    AddSignalToSession( newSession, startTime, endTime, signal );

                                    // Add the session to the DailyReport. We'll need to sort sessions afterward because of this. 
                                    day.Sessions.Add( newSession );
                                }
                            }
                        }
                    }

                    break;
                }
            }

            // Indicate whether the day has any detail data 
            day.HasDetailData = day.Sessions.Any( x => x.Signals.Count > 0 );

            // If a day has at least one session with Signal data, then remove any sessions that do not have Signal data.
            // This happens when a user has data for some sessions but also has a session for which there was no SD Card
            // in the machine, such as when they take a nap without putting the card back in the machine first (personal experience).
            if( day.HasDetailData )
            {
                day.Sessions.RemoveAll( x => x.Signals.Count == 0 );
            }

            foreach( var maskSession in day.Sessions )
            {
                // TODO: SpO2 and Pulse being invalid can actually be detected in the day's settings. Switch to that instead.
                // Remove all signals whose values are all out of range. This is how the AirSense indicates that 
                // there is no SpO2 or Pulse information when a pulse oximeter is not attached, and there may 
                // be other similar situations I haven't encountered yet (and in any case such signals are not valid). 
                maskSession.Signals.RemoveAll( x =>
                                                   (x.Name == SignalNames.SpO2 || x.Name == SignalNames.Pulse) &&
                                                   !x.Samples.Any( value => value >= x.MinValue )
                );
            }

            // If the day no longer has any sessions, stop processing it
            if( day.Sessions.Count == 0 )
            {
                return;
            }

            // If there is SpO2 and Pulse data, split those signals off into separate sessions for more logical grouping
            int sessionCount = day.Sessions.Count;
            for( int i = 0; i < sessionCount; i++ )
            {
                var session   = day.Sessions[ i ];
                var oxySignal = session.GetSignalByName( SignalNames.SpO2 );

                if( oxySignal != null )
                {
                    var newSession = new Session
                    {
                        StartTime  = session.StartTime,
                        EndTime    = session.EndTime,
                        Source     = session.Source,
                        SourceType = SourceType.PulseOximetry,
                    };


                    newSession.AddSignal( oxySignal );
                    session.Signals.Remove( oxySignal );

                    var pulseSignal = session.GetSignalByName( SignalNames.Pulse );
                    if( pulseSignal != null )
                    {
                        newSession.AddSignal( pulseSignal );
                        session.Signals.Remove( pulseSignal );
                    }

                    day.AddSession( newSession );
                }
            }

            Debug.Assert( !day.Sessions.Any( x => Math.Ceiling( x.Duration.TotalMinutes ) < _importSettings.MinimumSessionLength ) );

            // Sort the sessions by start time. This is only actually needed when we split a session above during
            // signal matching, but doesn't hurt anything when no sessions are split. 
            day.Sessions.Sort();

            var firstRecordedTime = DateTime.MaxValue;
            var lastRecordedTime  = DateTime.MinValue;

            // Now that each Session has all of its Signals added, each with correct StartTime and EndTime values, 
            // we can update the StartTime and EndTime of the Sessions. These values were previously set by the 
            // MaskOn/MaskOff values, which are convenience values used to match up session files and not accurate
            // start and end times. 
            if( day.HasDetailData )
            {
                foreach( var session in day.Sessions )
                {
                    // If there are no Signals recorded for this Session then it was likely that the user did
                    // not have the SD Card inserted when this Session was created, in which case there is no 
                    // need to adjust the session times or generate any additional calculated Signals. 
                    if( session.Signals.Count == 0 )
                    {
                        // We still want to keep track of the recorded Session times, though. 
                        lastRecordedTime  = DateUtil.Max( lastRecordedTime, session.EndTime );
                        firstRecordedTime = DateUtil.Min( firstRecordedTime, session.StartTime );

                        continue;
                    }

                    // Reset the session times to ensure that we don't keep artificial boundary times 
                    session.StartTime = DateTime.MaxValue;
                    session.EndTime   = DateTime.MinValue;

                    // Session start and end times must bound all signal start and end times 
                    foreach( var signal in session.Signals )
                    {
                        session.StartTime = DateUtil.Min( session.StartTime, signal.StartTime );
                        session.EndTime   = DateUtil.Max( session.EndTime, signal.EndTime );
                    }

                    // Generate any additional signals that we need
                    GenerateCalculatedSignals( day, session );

                    lastRecordedTime  = DateUtil.Max( lastRecordedTime, session.EndTime );
                    firstRecordedTime = DateUtil.Min( firstRecordedTime, session.StartTime );
                }

                // There is probably *some* value in keeping the "reported" start times and durations, but if so I cannot
                // think of what it is and doing so makes working with and displaying the data a lot more frustrating, so
                // we'll use the *actual* recorded session times instead. 
                day.RecordingStartTime = firstRecordedTime;
                day.RecordingEndTime   = lastRecordedTime;
                day.TotalSleepTime     = TimeSpan.FromSeconds( day.Sessions.Sum( x => x.Duration.TotalSeconds ) );

                // Calculate statistics (min, avg, median, max, etc) for each Signal
                CalculateSignalStatistics( day );

                // Remove all events that do not occur within a Session's timeframe. It seems that my AirSense 10 AutoSet
                // will sometimes flag a Hypopnea *immediately after* the session completes. Since the event does not occur
                // within the times bounded by any of the Session's Signals, it is invalid and needs to be removed. 
                day.Events.RemoveAll( evt => !day.Sessions.Any( sess => sess.StartTime <= evt.StartTime && sess.EndTime >= evt.StartTime ) );

                // Recalculate the EventSummary for the day
                day.UpdateEventSummary();

                // Generate events that are of interest which are not reported by the ResMed machine
                CustomEventGenerator.GenerateEvents( day, _importSettings );
            }
        }

        private static void GenerateCalculatedSignals( DailyReport day, Session session )
        {
            DerivedSignals.GenerateApneaIndexSignal( day, session );
            DerivedSignals.GenerateMissingRespirationSignals( day, session );
        }

        private static void CalculateSignalStatistics( DailyReport day )
        {
            // Not all Sessions will have data (such as when the SD Card wasn't inserted during a Session)
            // so attempt to find one that does.
            var firstSessionWithSignalData = day.Sessions.FirstOrDefault( x => x.Signals.Count > 0 );
            if( firstSessionWithSignalData == null )
            {
                return;
            }

            // Since each Session that contains Signal data (at least at this point in the import process)
            // should contain the same signal data as every other Session, we can just iterate through the
            // one Session's Signals to find out which ones to generate Statistics for. 
            foreach( var signal in firstSessionWithSignalData.Signals )
            {
                // Automatically calculate statistics for all Signals whose value range is zero or above
                if( signal.MinValue >= 0 && signal.MaxValue > signal.MinValue )
                {
                    // Skip the AHI signal, though
                    if( signal.Name == SignalNames.AHI )
                    {
                        continue;
                    }

                    var calculator = new SignalStatCalculator();
                    day.Statistics.Add( calculator.CalculateStats( signal.Name, day.Sessions ) );
                }
            }
        }

        private void LoadCheyneStokesEvents( string logFolder, DailyReport day )
        {
            var filenames = Directory.GetFiles( logFolder, "*_CSL.edf" );
            foreach( var filename in filenames )
            {
                var file               = EdfFile.Open( filename );
                var recordingStartTime = file.Header.StartTime.Value.AdjustImportTime( _importSettings );

                foreach( var annotationSignal in file.AnnotationSignals )
                {
                    double csrStartTime = double.MinValue;

                    foreach( var annotation in annotationSignal.Annotations )
                    {
                        // Discard all timekeeping annotations, those aren't relevant to anything. 
                        if( annotation.IsTimeKeepingAnnotation )
                        {
                            continue;
                        }

                        // Try to convert the annotation text into an Enum for easier processing. 
                        var eventFlag = ReportedEvent.FromEdfAnnotation( recordingStartTime, annotation );

                        // We don't need the "Recording Starts" annotations either 
                        if( eventFlag.Type == EventType.RecordingStarts )
                        {
                            continue;
                        }

                        if( annotation.Annotation.Equals( "CSR Start", StringComparison.OrdinalIgnoreCase ) )
                        {
                            csrStartTime = annotation.Onset;
                        }
                        else if( annotation.Annotation.Equals( "CSR End", StringComparison.OrdinalIgnoreCase ) )
                        {
                            // We're trusting the CPAP machine not to have overlapping CSR events
                            Debug.Assert( csrStartTime >= 0, "CSR Start/End pair mismatch found" );

                            var newEvent = new ReportedEvent
                            {
                                StartTime = recordingStartTime.AddSeconds( csrStartTime ),
                                Duration  = TimeSpan.FromSeconds( annotation.Onset - csrStartTime ),
                                Type      = EventType.CSR,
                            };

                            day.Events.Add( newEvent );

                            // Resetting the csrStartTime to an invalid value allows us to detect when Start/End pairs are mismatched
                            csrStartTime = double.MinValue;
                        }
                        else
                        {
                            Debug.Assert( false, $"Unhandled Event Type in {filename}: {annotation.Annotation}" );
                        }
                    }
                }
            }
        }

        private void LoadEventsAndAnnotations( string logFolder, DailyReport day )
        {
            var filenames = Directory.GetFiles( logFolder, "*_EVE.edf" );
            foreach( var filename in filenames )
            {
                var file               = EdfFile.Open( filename );
                var recordingStartTime = file.Header.StartTime.Value.AdjustImportTime( _importSettings );

                foreach( var annotationSignal in file.AnnotationSignals )
                {
                    foreach( var annotation in annotationSignal.Annotations )
                    {
                        // Discard all timekeeping annotations, those aren't relevant to anything. 
                        if( annotation.IsTimeKeepingAnnotation )
                        {
                            continue;
                        }

                        // Try to convert the annotation text into an Enum for easier processing. 
                        var eventFlag = ReportedEvent.FromEdfAnnotation( recordingStartTime, annotation );

                        // ResMed does not report a time for Hypopnea in Series 10 machines (although I've read
                        // that it does in S9 machines) but their own definition states that a minimum of ten
                        // seconds is part of the defining criteria, so if no time is reported we'll just set
                        // it to 10 seconds so that the time can be factored into the "Total Time in Apnea"
                        // calculations.
                        // ReSharper disable once ConvertIfStatementToSwitchStatement
                        if( eventFlag.Type == EventType.Hypopnea && eventFlag.Duration.TotalSeconds <= double.Epsilon )
                        {
                            eventFlag.Duration = TimeSpan.FromSeconds( 10 );
                        }

                        // We don't need the "Recording Starts" annotations either 
                        if( eventFlag.Type == EventType.RecordingStarts )
                        {
                            continue;
                        }

                        // Add the event flags to the current day
                        day.Events.Add( eventFlag );
                    }
                }
            }

            day.Events.Sort();
        }

        private List<DailyReport> LoadIndexAndSettings( string filename, DateTime minDate, DateTime maxDate )
        {
            var days = new List<DailyReport>();

            var file = EdfFile.Open( filename );

            if( file.Signals.Count == 0 )
            {
                return days;
            }

            // The STR.edf file is essentially a vertical table containing the settings data for each
            // recorded day. We need to transpose that data and use it to create a DailyReport for 
            // each available day. 
            for( int i = 0; i < file.Signals[ 0 ].Samples.Count; i++ )
            {
                // Convert the vertical table into a hashtable for easy lookup
                var lookup = new Dictionary<string, double>();
                for( int j = 0; j < file.Signals.Count; j++ )
                {
                    lookup[ file.Signals[ j ].Label ] = file.Signals[ j ].Samples[ i ];
                }

                // Read in and process the settings for a single day
                var day = ReadDailyReport( lookup );

                // A DailyReport instance will not always be returned (such as when there is no 
                // data stored for a given day), but if one is then add it to the list. 
                if( day != null )
                {
                    day.MachineInfo = _machineInfo;

                    days.Add( day );
                }
            }

            // Mask On and Mask Off times are stored as the number of seconds since the day started.
            // Remember that according to ResMed, the day starts at 12pm (noon) instead of the more conventional 
            // and sane 12am (midnight).
            // There will be a maximum of ten MaskOn/MaskOff events per day (always true?)
            var maskOnSignal  = file.GetSignalByName( "MaskOn",  "Mask On" );
            var maskOffSignal = file.GetSignalByName( "MaskOff", "Mask Off" );

            // There will be an even number of MaskOn/MaskOff times for each day
            var numberOfEntriesPerDay = maskOnSignal.Samples.Count / days.Count;
            Debug.Assert( maskOnSignal.Samples.Count % numberOfEntriesPerDay == 0, "Invalid calculation of Number of Sessions Per Day" );

            for( int dayIndex = 0; dayIndex < days.Count; dayIndex++ )
            {
                var day = days[ dayIndex ];

                if( day.TotalSleepTime.TotalMinutes < 5 )
                {
                    continue;
                }

                for( int i = 0; i < day.MaskEvents; i++ )
                {
                    var sampleIndex = dayIndex * numberOfEntriesPerDay + i;

                    // Stop processing MaskOn/MaskOff when we encounter a -1
                    if( maskOnSignal.Samples[ sampleIndex ] < 0 || maskOffSignal.Samples[ sampleIndex ] < 0 )
                    {
                        break;
                    }

                    // Mask times are stored as the number of seconds since the "day" started. Remember that
                    // the ResMed "day" starts at 12pm (noon) and continues until the next calendar day at 12pm.
                    var maskOn  = day.ReportDate.AddMinutes( maskOnSignal.Samples[ sampleIndex ] ).AdjustImportTime( _importSettings );
                    var maskOff = day.ReportDate.AddMinutes( maskOffSignal.Samples[ sampleIndex ] ).AdjustImportTime( _importSettings );

                    // Discard any sessions that are shorter than the specified minimum duration.
                    // Note that this isn't a complete solution to short sessions: if a Session is split because the MaskOn/MaskOff
                    // times do not match the Signal files, a short Session might still be created and that will have to be 
                    // checked as well (search for "discontinuous Session" in this file for more details).
                    if( Math.Ceiling( maskOff.Subtract( maskOn ).TotalMinutes ) < _importSettings.MinimumSessionLength )
                    {
                        continue;
                    }

                    var session = new Session()
                    {
                        SourceType = SourceType.CPAP,
                        StartTime  = maskOn,
                        EndTime    = maskOff,
                    };

                    day.Sessions.Add( session );
                }

                if( day.Sessions.Count > 0 )
                {
                    day.RecordingStartTime = day.Sessions.Min( x => x.StartTime );
                    day.RecordingEndTime   = day.Sessions.Max( x => x.EndTime );
                }
            }

            // Remove all days that are too short to be valid or are otherwise invalid
            RemoveInvalidDays( days );

            // Remove days that don't match the provided range. It's less efficient to do this after we've already 
            // gathered the basic day information, but it keeps the code much cleaner and more readable, and this 
            // isn't exactly a performance-critical section of code ;)
            FilterDaysByDate( days, minDate, maxDate );

            return days;
        }

        /// <summary>
        /// Reads the statistics, settings, and other information from the stored data
        /// </summary>
        private DailyReport ReadDailyReport( Dictionary<string, double> data )
        {
            // TODO: Retain all raw settings data on import
            // I've tried my best to decode what all of the data means, and convert it to meaningful typed
            // values exposed in a reasonable manner, but it's highly likely that there's something I didn't
            // understand correctly, not to mention fields that are different for different models, so the
            // raw data should be kept available for the consumer of this library to make use of if needs be.

            var settings = ReadMachineSettings( data );

            // ReadMachineSettings() will return NULL in the special situation that the stored settings are 
            // all invalid (all negative numbers), which is indicative of a special case where ResMed has 
            // written the STR.edf file for a day that has not yet had any recorded data added to it. 
            if( settings == null )
            {
                return null;
            }

            var day = new DailyReport
            {
                MachineInfo    = _machineInfo,
                ReportDate     = new DateTime( 1970, 1, 1 ).AddDays( data[ "Date" ] ).AddHours( 12 ),
                Settings       = settings,
                EventSummary   = ReadEventsSummary( data ),
                StatsSummary   = ReadStatsSummary( data ),
                MaskEvents     = (int)(data[ "MaskEvents" ] / 2),
                TotalSleepTime = TimeSpan.FromMinutes( data[ "Duration" ] ),
            };

            return day;
        }

        private static EventSummary ReadEventsSummary( Dictionary<string, double> data )
        {
            Debug.Assert( data.ContainsKey( "AHI" ) );

            var summary = new EventSummary
            {
                AHI                      = getValue( "AHI" ),
                ApneaIndex               = getValue( "AI" ),
                HypopneaIndex            = getValue( "HI" ),
                ObstructiveApneaIndex    = getValue( "OAI" ),
                CentralApneaIndex        = getValue( "CAI" ),
                UnclassifiedApneaIndex   = getValue( "UAI" ),
                RespiratoryArousalIndex  = getValue( "RIN" ),
                CheynesStokesRespiration = getValue( "CSR" ),
            };

            return summary;

            // Because different models (AirSense 10 vs. AirCurve 10 for instance) provide different
            // summary data, we need to check each key to make sure it is available and return a
            // default value if it is not. 
            double getValue( string key )
            {
                return data.TryGetValue( key, out double value ) ? value : 0.0;
            }
        }

        private static StatisticsSummary ReadStatsSummary( Dictionary<string, double> data )
        {
            Debug.Assert( data.ContainsKey( "Leak.95" ) );

            var summary = new StatisticsSummary
            {
                Leak95     = getValue( "Leak.95" ),
                LeakMedian = getValue( "Leak.50" ),

                RespirationRateMax    = getValue( "RespRate.Max" ),
                RespirationRate95     = getValue( "RespRate.95" ),
                RespirationRateMedian = getValue( "RespRate.50" ),

                MinuteVentilationMax    = getValue( "MinVent.Max" ),
                MinuteVentilation95     = getValue( "MinVent.95" ),
                MinuteVentilationMedian = getValue( "MinVent.50" ),

                TidalVolumeMax    = getValue( "TidVol.Max" ) * 1000.0,
                TidalVolume95     = getValue( "TidVol.95" ) * 1000.0,
                TidalVolumeMedian = getValue( "TidVol.50" ) * 1000.0,

                PressureMax    = getValue( "MaskPress.Max" ),
                Pressure95     = getValue( "MaskPress.95" ),
                PressureMedian = getValue( "MaskPress.50" ),

                TargetIpapMax    = getValue( "TgtIPAP.50" ),
                TargetIpap95     = getValue( "TgtIPAP.50" ),
                TargetIpapMedian = getValue( "TgtIPAP.50" ),

                TargetEpapMax    = getValue( "TgtEPAP.50" ),
                TargetEpap95     = getValue( "TgtEPAP.50" ),
                TargetEpapMedian = getValue( "TgtEPAP.50" ),
            };

            return summary;

            // Because different models (AirSense 10 vs. AirCurve 10 for instance) provide different
            // summary data, we need to check each key to make sure it is available and return a
            // default value if it is not. 
            double getValue( string key )
            {
                return data.TryGetValue( key, out double value ) ? value : 0.0;
            }
        }

        private static MachineSettings ReadMachineSettings( Dictionary<string, double> data )
        {
            var settings = new MachineSettings();

            OperatingMode operatingMode = OperatingMode.UNKNOWN;

            if( data.TryGetValue( "CPAP_MODE", out double legacyMode ) )
            {
                switch( (int)legacyMode )
                {
                    case 1:
                    case 2:
                        operatingMode = OperatingMode.Apap;
                        break;
                    case 3:
                        operatingMode = OperatingMode.Cpap;
                        break;
                    default:
                        operatingMode = OperatingMode.UNKNOWN;
                        break;
                }
            }
            else
            {
                var mode = (int)data.GetValue( "Mode" );

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if( s_modeMapping.TryGetValue( mode, out OperatingMode mappedMode ) )
                {
                    operatingMode = mappedMode;
                }
                else
                {
                    operatingMode = OperatingMode.Cpap;
                }
            }

            settings[ SettingNames.Mode ] = operatingMode;

            switch( operatingMode )
            {
                case OperatingMode.Cpap:
                    break;
                case OperatingMode.Apap:
                    settings[ SettingNames.MinPressure ] = data[ "S.AS.MinPress" ];
                    settings[ SettingNames.MaxPressure ] = data[ "S.AS.MaxPress" ];
                    break;
                case OperatingMode.Asv:
                    settings[ SettingNames.RampPressure ]       = data[ "S.AV.StartPress" ];
                    settings[ SettingNames.MinPressureSupport ] = data[ "S.AV.MinPS" ];
                    settings[ SettingNames.MaxPressureSupport ] = data[ "S.AV.MaxPS" ];
                    settings[ SettingNames.EPAP ]               = data[ "S.AV.EPAP" ];
                    settings[ SettingNames.EpapMin ]            = data[ "S.AA.MinEPAP" ];
                    settings[ SettingNames.EpapMax ]            = data[ "S.AA.MaxEPAP" ];
                    settings[ SettingNames.IpapMin ]            = data[ "S.AV.EPAP" ] + data[ "S.AV.MinPS" ];
                    settings[ SettingNames.IpapMax ]            = data[ "S.AV.EPAP" ] + data[ "S.AV.MaxPS" ];
                    break;
                case OperatingMode.AsvVariableEpap:
                    settings[ SettingNames.RampPressure ]       = data[ "S.AA.StartPress" ];
                    settings[ SettingNames.MinPressureSupport ] = data[ "S.AA.MinPS" ];
                    settings[ SettingNames.MaxPressureSupport ] = data[ "S.AA.MaxPS" ];
                    settings[ SettingNames.EPAP ]               = data[ "S.AA.MinEPAP" ];
                    settings[ SettingNames.EpapMin ]            = data[ "S.AA.MinEPAP" ];
                    settings[ SettingNames.EpapMax ]            = data[ "S.AA.MaxEPAP" ];
                    settings[ SettingNames.IpapMin ]            = data[ "S.AV.EPAP" ] + data[ "S.AA.MinPS" ];
                    settings[ SettingNames.IpapMax ]            = data[ "S.AV.EPAP" ] + data[ "S.AA.MaxPS" ];
                    break;
                case OperatingMode.Avaps:
                {
                    settings[ SettingNames.RampPressure ]       = data[ "S.i.StartPress" ];
                    settings[ SettingNames.MinPressureSupport ] = data[ "S.i.MinPS" ];
                    settings[ SettingNames.MaxPressureSupport ] = data[ "S.i.MaxPS" ];
                    settings[ SettingNames.EpapAuto ]           = data[ "S.i.EPAPAuto" ] > 0.5;
                    settings[ SettingNames.EPAP ]               = data[ "S.i.EPAP" ];
                    settings[ SettingNames.EpapMin ]            = data[ "S.i.EPAP" ];
                    settings[ SettingNames.EpapMax ]            = data[ "S.i.EPAP" ];

                    if( settings.GetValue<bool>( SettingNames.EpapAuto ) )
                    {
                        settings[ SettingNames.EpapMin ] = data[ "S.i.MinEPAP" ];
                        settings[ SettingNames.EpapMax ] = data[ "S.i.MaxEPAP" ];

                        settings[ SettingNames.IPAP ]    = data[ "S.i.MinEPAP" ] + data[ "S.i.MinPS" ];
                        settings[ SettingNames.IpapMin ] = data[ "S.i.MinEPAP" ] + data[ "S.i.MinPS" ];
                        settings[ SettingNames.IpapMax ] = data[ "S.i.MaxEPAP" ] + data[ "S.i.MaxPS" ];
                    }
                    else
                    {
                        settings[ SettingNames.IPAP ]    = data[ "S.i.EPAP" ] + data[ "S.i.MinPS" ];
                        settings[ SettingNames.IpapMin ] = data[ "S.i.EPAP" ] + data[ "S.i.MinPS" ];
                        settings[ SettingNames.IpapMax ] = data[ "S.i.EPAP" ] + data[ "S.i.MaxPS" ];
                    }
                    break;
                }
                default:
                    throw new NotSupportedException( $"Operating Mode {operatingMode} is not yet supported" );
            }

            if( data.TryGetValue( "S.EPR.EPREnable", out double eprEnabledValue ) )
            {
                settings[ SettingNames.EprEnabled ]   = eprEnabledValue >= 0.5;
                settings[ SettingNames.EprLevel ]     = (int)data[ "S.EPR.Level" ];
                settings[ SettingNames.EprMode ]      = (EprType)(int)(data[ "S.EPR.EPRType" ] + 1);
                settings[ SettingNames.ResponseType ] = (AutoSetResponseType)(int)data[ "S.AS.Comfort" ];

                // settings[ SettingNames.RampPressure ] = data[ "S.AS.StartPress" ];
                // settings[ SettingNames.MaxPressure ]  = data[ "S.AS.MaxPress" ];
                // settings[ SettingNames.MinPressure ]  = data[ "S.AS.MinPress" ];
            }
            else
            {
                settings[ SettingNames.EprEnabled ] = false;
            }

            settings[ SettingNames.Pressure ]     = data[ "S.C.Press" ];
            settings[ SettingNames.RampMode ]     = (RampModeType)(int)data[ "S.RampEnable" ];
            settings[ SettingNames.RampPressure ] = data[ "S.C.StartPress" ];
            settings[ SettingNames.RampTime ]     = data[ "S.RampTime" ];

            settings[ SettingNames.SmartStart ]          = data[ "S.SmartStart" ] > 0.5 ? OnOffType.On : OnOffType.Off;
            settings[ SettingNames.AntibacterialFilter ] = data[ "S.ABFilter" ] >= 0.5;

            settings[ SettingNames.HeatedTubePresent ]  = data[ "HeatedTube" ] > 0.5;
            settings[ SettingNames.HumidifierAttached ] = data[ "Humidifier" ] > 0.5;
            settings[ SettingNames.ClimateControl ]     = (ClimateControlType)(int)data[ "S.ClimateControl" ];
            settings[ SettingNames.HumidifierMode ]     = data[ "Humidifier" ] > 0.5 && data[ "S.HumEnable" ] > 0.5 ? OnOffType.On : OnOffType.Off;
            settings[ SettingNames.HumidityLevel ]      = data[ "S.HumLevel" ];
            settings[ SettingNames.HeatedTubeEnabled ]  = data[ "S.TempEnable" ] > 0.5;
            settings[ SettingNames.TubeTemperature ]    = data[ "S.Temp" ] * 1.8 + 32; // Converted from Celsius to Fahrenheit

            settings[ SettingNames.MaskType ] = (MaskType)(int)data[ "S.Mask" ];

            settings[ SettingNames.EssentialsMode ] = data[ "S.PtAccess" ] > 0.5 ? EssentialsMode.On : EssentialsMode.Plus;

            return settings;
        }

        private static void FilterDaysByDate( List<DailyReport> days, DateTime minDate, DateTime maxDate )
        {
            int dayIndex = 0;
            while( dayIndex < days.Count )
            {
                var date = days[ dayIndex ].ReportDate.Date;

                if( date < minDate )
                {
                    days.RemoveAt( dayIndex );
                    continue;
                }

                if( date > maxDate )
                {
                    days.RemoveAt( dayIndex );
                    continue;
                }

                dayIndex += 1;
            }
        }

        private static void RemoveInvalidDays( List<DailyReport> days )
        {
            int dayIndex = 0;
            while( dayIndex < days.Count )
            {
                if( days[ dayIndex ].TotalSleepTime.TotalMinutes <= 5 )
                {
                    days.RemoveAt( dayIndex );
                    continue;
                }

                dayIndex += 1;
            }
        }

        private static void AddSignalToSession( Session session, DateTime startTime, DateTime endTime, EdfStandardSignal fileSignal )
        {
            // Rename signals to their "standard" names. Among other things, this lets us standardize the 
            // data even when there might be slight differences in signal names among various machine models.
            var signalName = SignalNames.GetStandardName( fileSignal.Label.Value );

            Signal signal = session.GetSignalByName( signalName, StringComparison.Ordinal );

            if( signal != null )
            {
                throw new Exception( $"The session starting at {session.StartTime:g} already contains a Signal named '{signalName}'" );
            }

            signal = new Signal
            {
                Name              = signalName,
                StartTime         = startTime,
                EndTime           = endTime,
                FrequencyInHz     = fileSignal.FrequencyInHz,
                MinValue          = fileSignal.PhysicalMinimum,
                MaxValue          = fileSignal.PhysicalMaximum,
                UnitOfMeasurement = fileSignal.PhysicalDimension,
            };

            signal.Samples.AddRange( fileSignal.Samples );

            session.Signals.Add( signal );
        }

		#endregion
    }
}