﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using StagPoint.EDF.Net;

namespace cpaplib
{
	public class CpapDataLoader
	{
		public MachineIdentification MachineID = new MachineIdentification();

		public List<DailyReport> Days { get; } = new List<DailyReport>();

		private static string[] expectedFiles = new[]
		{
			"STR.edf",
			"Identification.tgt",
		};

		private static string[] expectedFolders = new[]
		{
			// "SETTINGS",
			"DATALOG",
		};

		public void LoadFromFolder( string folderPath, DateTime? minDate = null, DateTime? maxDate = null )
		{
			EnsureCorrectFolderStructure( folderPath );
			LoadMachineIdentificationInfo( folderPath );

			var indexFilename = Path.Combine( folderPath, "STR.edf" );
			LoadIndexAndSettings( indexFilename, minDate, maxDate );

#if ALLOW_ASYNC
			var tasks = new Task[ Days.Count ];

			for( int i = 0; i < Days.Count; i++ )
			{
				var day = Days[ i ];
				
				tasks[i] = Task.Run( () =>
				{
					LoadSessionsForDay( folderPath, day );
				} );
			}

			Task.WaitAll( tasks );
#else
			foreach( var day in Days )
			{
				LoadSessionsForDay( folderPath, day );
			}
#endif
		}

		private void LoadMachineIdentificationInfo( string rootFolder )
		{
			var filename = Path.Combine( rootFolder, "Identification.tgt" );
			MachineID = MachineIdentification.ReadFrom( filename );
		}

		public static bool HasCorrectFolderStructure( string rootFolder )
		{
			foreach( var folder in expectedFolders )
			{
				var directoryPath = Path.Combine( rootFolder, folder );
				if( !Directory.Exists( directoryPath ) )
				{
					return false;
				}
			}

			foreach( var filename in expectedFiles )
			{
				var filePath = Path.Combine( rootFolder, filename );
				if( !File.Exists( filePath ) )
				{
					return false;
				}
			}

			return true;
		}

		private void EnsureCorrectFolderStructure( string rootFolder )
		{
			foreach( var folder in expectedFolders )
			{
				var directoryPath = Path.Combine( rootFolder, folder );
				if( !Directory.Exists( directoryPath ) )
				{
					throw new DirectoryNotFoundException( $"Directory {directoryPath} does not exist" );
				}
			}

			foreach( var filename in expectedFiles )
			{
				var filePath = Path.Combine( rootFolder, filename );
				if( !File.Exists( filePath ) )
				{
					throw new FileNotFoundException( $"File {filePath} does not exist" );
				}
			}
		}

		private void LoadSessionsForDay( string rootFolder, DailyReport day )
		{
			var logFolder = Path.Combine( rootFolder, $@"DATALOG\{day.ReportDate:yyyyMMdd}" );
			if( !Directory.Exists( logFolder ) )
			{
				throw new DirectoryNotFoundException( $"Could not find the session directory for {logFolder}" );
			}

			LoadEventsAndAnnotations( logFolder, day );

			var filenames = Directory.GetFiles( logFolder, "*.edf" );
			foreach( var filename in filenames )
			{
				var baseFilename = Path.GetFileNameWithoutExtension( filename );

				// EVE and CSL files are handled separately
				var ignoreThisFile =
					baseFilename.EndsWith( "_CSL", StringComparison.InvariantCultureIgnoreCase ) ||
					baseFilename.EndsWith( "_EVE", StringComparison.InvariantCultureIgnoreCase );
				if( ignoreThisFile )
				{
					continue;
				}

				// The file's date, extracted from the filename, will be used to search for the correct session.
				// The date/time is incorrect for any other purpose (I think)
				var fileDate = DateTime
				               .ParseExact( baseFilename.Substring( 0, baseFilename.Length - 4 ), "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture )
				               .Trim( TimeSpan.TicksPerMinute );

				foreach( var session in day.Sessions )
				{
					// If the file's extracted date does not intersect with the session, then it's not relevant to that session 
					if( session.StartTime > fileDate || session.EndTime < fileDate )
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
							// We don't need to keep the CRC checksum signals 
							if( signal.Label.Value.StartsWith( "crc", StringComparison.InvariantCultureIgnoreCase ) )
							{
								continue;
							}

							// Not every signal within a Session will have the same start and end time as the others
							// because of differences in sampling rate, so we keep track of the start time and end
							// time of each Signal separately. 
							var startTime = header.StartTime.Value;
							var endTime   = startTime.AddSeconds( header.NumberOfDataRecords * header.DurationOfDataRecord );

							// Add the signal to the current session
							session.AddSignal( startTime, endTime, signal );
						}
					}

					break;
				}
			}
			
			// Now that each Session has all of its Signals added, each with correct StartTime and EndTime values, 
			// we can update the StartTime and EndTime of the Sessions. These values were previously set by the 
			// MaskOn/MaskOff values, which are convenience values used to match up session files and not accurate
			// start and end times. 
			foreach( var session in day.Sessions )
			{
				// Reset the session times to ensure that we don't keep artificial boundary times 
				session.StartTime = DateTime.MaxValue;
				session.EndTime   = DateTime.MinValue;
				
				// Session start and end times must bound all signal start and end times 
				foreach( var signal in session.Signals )
				{
					session.StartTime = DateUtil.Min( session.StartTime, signal.StartTime );
					session.EndTime   = DateUtil.Max( session.EndTime, signal.EndTime );
				}
			}

			CalculateSignalStatistics( day );
		}
		
		private void CalculateSignalStatistics( DailyReport day )
		{
			// We don't actually need double precision for any of these signals (biomedical signals just
			// don't need that kind of precision) and single-precision floats sort faster.
			ListEx<float> samples = new ListEx<float>();

			day.Statistics.MaskPressure       = calculateStatistics( "Mask Pressure" );
			day.Statistics.TherapyPressure    = calculateStatistics( "Therapy Pressure" );
			day.Statistics.ExpiratoryPressure = calculateStatistics( "Expiratory Pressure" );
			day.Statistics.Leak               = calculateStatistics( "Leak Rate" );
			day.Statistics.RespirationRate    = calculateStatistics( "Respiration Rate" );
			day.Statistics.TidalVolume        = calculateStatistics( "Tidal Volume" );
			day.Statistics.MinuteVent         = calculateStatistics( "Minute Vent" );
			day.Statistics.Snore              = calculateStatistics( "Snore" );
			day.Statistics.FlowLimit          = calculateStatistics( "Flow Limit" );
			day.Statistics.Pulse              = calculateStatistics( "Pulse" );
			day.Statistics.SpO2               = calculateStatistics( "SpO2" );

			SignalStatistics calculateStatistics( string signalName )
			{
				samples.Clear();
				
				// Signal index will be consistent for all sessions, so grab that to avoid having to look it up each time
				var signalIndex = day.Sessions[ 0 ].Signals.FindIndex( x => x.Name.Equals( signalName, StringComparison.Ordinal ) );

				// The signal wasn't found... Typically only happens when a session is essentially empty because the
				// base session data wasn't found. Should never get to this point if that's the case, but worth checking.
				if( signalIndex == -1 )
				{
					throw new IndexOutOfRangeException( $"Failed to find signal {signalName} for day {day.ReportDate.ToShortDateString()}" );
				}
				
				// Copy all available samples from all sessions into a single array that can be sorted and
				// used to calculate the statistics. 
				foreach( var session in day.Sessions )
				{
					var sourceSamples = session.Signals[ signalIndex ].Samples;
					samples.GrowIfNeeded( sourceSamples.Count );

					foreach( var sample in sourceSamples )
					{
						samples.Add( (float)sample );
					}
				}
				
#if ALLOW_UNSAFE
				// The samples need to be sorted in order to determine things like 95th Percentile, etc.  
				if( samples.Count <= 1000 )
				{
					samples.Sort();
				}
				else
				{
					// A radix sort is *blazing fast* on large arrays. Much faster than the built-in sort, 
					// but uses twice the amount of memory in exchange (because it's not an in-place sort). 
					RadixSort.Sort( samples );
				}
#else
				samples.Sort();
#endif


				var stats = new SignalStatistics
				{
					Minimum      = samples[ (int)(samples.Count * 0.01) ],
					Average      = samples.Average(),
					Maximum      = samples.Max(),
					Median       = samples[ samples.Count / 2 ],
					Percentile95 = samples[ (int)( samples.Count * 0.95 ) ],
					Percentile99 = samples[ (int)( samples.Count * 0.995 ) ],
				};

				return stats;
			}
		}
		
		private void LoadEventsAndAnnotations( string logFolder, DailyReport day )
		{
			var filenames = Directory.GetFiles( logFolder, "*_EVE.edf" );
			foreach( var filename in filenames )
			{
				var file = EdfFile.Open( filename );
				day.RecordingStartTime = file.Header.StartTime;
				
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
						var eventFlag = EventFlag.FromEdfAnnotation( day.RecordingStartTime, annotation );
						
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
		}

		private void LoadIndexAndSettings( string filename, DateTime? minDate, DateTime? maxDate )
		{
			var file = EdfFile.Open( filename );

			if( file.Signals.Count == 0 )
			{
				return;
			}

			// The STR.edf file is essentially a vertical table containing the settings data for each
			// recorded day. We need to transpose that data and use it to create a DailyReport for 
			// each available day. 
			for( int i = 0; i < file.Signals[ 0 ].Samples.Count; i++ )
			{
				// Gather a hash table of settings for a single day from across the signals 
				var lookup = new Dictionary<string, double>();
				for( int j = 0; j < file.Signals.Count; j++ )
				{
					var signalName = SignalNames.GetStandardName( file.Signals[ j ].Label );
					lookup[ signalName ] = file.Signals[ j ].Samples[ i ];
				}

				// Read in and process the settings for a single day
				var settings = DailyReport.Read( lookup );

				Days.Add( settings );
			}

			// Mask On and Mask Off times are stored as the number of seconds since the day started.
			// Remember that according to ResMed, the day starts at 12pm (noon) instead of the more conventional 
			// and sane 12am (midnight).
			// There will be a maximum of ten MaskOn/MaskOff events per day (always true?)
			var maskOnSignal  = file.GetSignalByName( "MaskOn",  "Mask On" );
			var maskOffSignal = file.GetSignalByName( "MaskOff", "Mask Off" );

			// There will be an even number of MaskOn/MaskOff times for each day
			var numberOfEntriesPerDay = maskOnSignal.Samples.Count / Days.Count;
			Debug.Assert( maskOnSignal.Samples.Count % numberOfEntriesPerDay == 0, "Invalid calculation of Number of Sessions Per Day" );

			for( int dayIndex = 0; dayIndex < Days.Count; dayIndex++ )
			{
				var day = Days[ dayIndex ];

				if( day.Duration.TotalMinutes < 5 )
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
					var maskOn   = day.ReportDate.AddMinutes( maskOnSignal.Samples[ sampleIndex ] );
					var maskOff  = day.ReportDate.AddMinutes( maskOffSignal.Samples[ sampleIndex ] );
					var duration = maskOffSignal.Samples[ sampleIndex ] - maskOnSignal.Samples[ sampleIndex ];

					// Discard empty sessions
					if( maskOff.Subtract( maskOn ).TotalMinutes < 5 )
					{
						continue;
					}

					var session = new MaskSession()
					{
						StartTime = maskOn,
						EndTime   = maskOff,
						duration  = duration
					};

					day.Sessions.Add( session );
				}
			}

			// Remove all days that are too short to be valid or are otherwise invalid
			RemoveInvalidDays();

			// Remove days that don't match the provided range. It's less efficient to do this after we've already 
			// gathered the basic day information, but it keeps the code much cleaner and more readable, and this 
			// isn't exactly a performance-critical section of code ;)
			FilterDaysByDate( minDate, maxDate );
		}

		private void FilterDaysByDate( DateTime? minDate, DateTime? maxDate )
		{
			int dayIndex = 0;
			if( minDate.HasValue || maxDate.HasValue )
			{
				while( dayIndex < Days.Count )
				{
					var date = Days[ dayIndex ].ReportDate;

					if( minDate.HasValue && date < minDate )
					{
						Days.RemoveAt( dayIndex );
						continue;
					}

					if( maxDate.HasValue && date > maxDate )
					{
						Days.RemoveAt( dayIndex );
						continue;
					}

					dayIndex += 1;
				}
			}
		}

		private void RemoveInvalidDays()
		{
			int dayIndex = 0;
			while( dayIndex < Days.Count )
			{
				if( Days[ dayIndex ].Duration.TotalMinutes <= 5 )
				{
					Days.RemoveAt( dayIndex );
					continue;
				}

				dayIndex += 1;
			}
		}
	}
}
