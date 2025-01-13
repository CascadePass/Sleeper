using System.Diagnostics;
using System.Text;

using cpap_app.Converters;
using cpap_app.Helpers;

using cpaplib;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

// ReSharper disable UseIndexFromEndExpression

// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable StringLiteralTypo

namespace cpaplib_tests;

[TestClass]
public class PRS1_tests
{
	#region Private fields 
	
	private const  string SD_CARD_ROOT  = @"D:\Data Files\CPAP Sample Data\REMStar Auto\P-Series\";
	private static string SOURCE_FOLDER = Path.Combine( SD_CARD_ROOT, "P1192913945CE" );

	private static Dictionary<string, string?> _modelToProductName = new Dictionary<string, string?>()
	{
		{ "760P", "BiPAP Auto (System One 60 Series)" },
		{ "761P", "BiPAP Auto (System One 60 Series)" },
		{ "750P", "BiPAP Auto (System One)" },
		{ "960P", "BiPAP autoSV Advanced (System One 60 Series)" },
		{ "961P", "BiPAP autoSV Advanced (System One 60 Series)" },
		{ "960T", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
		{ "961TCA", "BiPAP autoSV Advanced 30 (System One 60 Series)" },
		{ "950P", "BiPAP AutoSV Advanced System One" },
		{ "951P", "BiPAP AutoSV Advanced System One" },
		{ "1160P", "BiPAP AVAPS 30 (System One 60 Series)" },
		{ "660P", "BiPAP Pro (System One 60 Series)" },
		{ "650P", "BiPAP Pro (System One)" },
		{ "1061401", "BiPAP S/T (C Series)" },
		{ "1061T", "BiPAP S/T 30 (System One 60 Series)" },
		{ "501V", "Dorma 500 Auto (System One 60 Series)" },
		{ "420X150C", "DreamStation 2 Advanced CPAP" },
		{ "410X150C", "DreamStation 2 CPAP" },
		{ "700X110", "DreamStation Auto BiPAP" },
		{ "700X120", "DreamStation Auto BiPAP" },
		{ "700X130", "DreamStation Auto BiPAP" },
		{ "700X150", "DreamStation Auto BiPAP" },
		{ "500X110", "DreamStation Auto CPAP" },
		{ "500X120", "DreamStation Auto CPAP" },
		{ "500X130", "DreamStation Auto CPAP" },
		{ "500X150", "DreamStation Auto CPAP" },
		{ "500X180", "DreamStation Auto CPAP" },
		{ "500X140", "DreamStation Auto CPAP with A-Flex" },
		{ "501X120", "DreamStation Auto CPAP with P-Flex" },
		{ "900X110", "DreamStation BiPAP autoSV" },
		{ "900X120", "DreamStation BiPAP autoSV" },
		{ "900X150", "DreamStation BiPAP autoSV" },
		{ "1130X110", "DreamStation BiPAP AVAPS 30" },
		{ "1130X200", "DreamStation BiPAP AVAPS 30" },
		{ "1131X150", "DreamStation BiPAP AVAPS 30 AE" },
		{ "600X110", "DreamStation BiPAP Pro" },
		{ "600X150", "DreamStation BiPAP Pro" },
		{ "1030X110", "DreamStation BiPAP S/T 30" },
		{ "1030X150", "DreamStation BiPAP S/T 30 with AAM" },
		{ "200X110", "DreamStation CPAP" },
		{ "400X110", "DreamStation CPAP Pro" },
		{ "400X120", "DreamStation CPAP Pro" },
		{ "400X130", "DreamStation CPAP Pro" },
		{ "400X150", "DreamStation CPAP Pro" },
		{ "401X150", "DreamStation CPAP Pro with Auto-Trial" },
		{ "400G110", "DreamStation Go" },
		{ "500G110", "DreamStation Go Auto" },
		{ "500G120", "DreamStation Go Auto" },
		{ "500G150", "DreamStation Go Auto" },
		{ "502G150", "DreamStation Go Auto" },
		{ "560P", "REMstar Auto (System One 60 Series)" },
		{ "560PBT", "REMstar Auto (System One 60 Series)" },
		{ "561P", "REMstar Auto (System One 60 Series)" },
		{ "562P", "REMstar Auto (System One 60 Series)" },
		{ "550P", "REMstar Auto (System One)" },
		{ "551P", "REMstar Auto (System One)" },
		{ "552P", "REMstar Auto (System One)" },
		{ "261CA", "REMstar Plus (System One 60 Series)" },
		{ "261P", "REMstar Plus (System One 60 Series)" },
		{ "251P", "REMstar Plus (System One)" },
		{ "460P", "REMstar Pro (System One 60 Series)" },
		{ "460PBT", "REMstar Pro (System One 60 Series)" },
		{ "461CA", "REMstar Pro (System One 60 Series)" },
		{ "461P", "REMstar Pro (System One 60 Series)" },
		{ "462P", "REMstar Pro (System One 60 Series)" },
		{ "450P", "REMstar Pro (System One)" },
		{ "451P", "REMstar Pro (System One)" },
		{ "452P", "REMstar Pro (System One)" },
	};
	
	#endregion

	//[TestMethod]
	public void CanImportFromRootFolder()
	{
        if (!File.Exists(SD_CARD_ROOT))
        {
            Assert.Inconclusive($"Test path '{SD_CARD_ROOT}' does not exist.");
        }

        var loader = new PRS1DataLoader();
		Assert.IsTrue( loader.HasCorrectFolderStructure( SD_CARD_ROOT ) );
		Assert.IsNotNull( loader.LoadMachineIdentificationInfo( SD_CARD_ROOT ) );
		
		var days = loader.LoadFromFolder( SD_CARD_ROOT );
		Assert.IsNotNull( days );
		Assert.IsTrue( days.Count > 0 );
	}

	//[TestMethod]
	public void CanImportDateRangeOnly()
	{
        if (!File.Exists(SD_CARD_ROOT))
        {
            Assert.Inconclusive($"Test path '{SD_CARD_ROOT}' does not exist.");
        }
        
		var loader = new PRS1DataLoader();
		Assert.IsTrue( loader.HasCorrectFolderStructure( SD_CARD_ROOT ) );
		Assert.IsNotNull( loader.LoadMachineIdentificationInfo( SD_CARD_ROOT ) );

		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );

        if (!File.Exists(propertyFilePath))
        {
            Assert.Inconclusive($"Test path '{propertyFilePath}' does not exist.");
        }

        var fields     = ReadKeyValueFile( propertyFilePath );
		var firstDate  = DateTime.UnixEpoch.AddSeconds( int.Parse( fields[ "FirstDate" ] ) ).ToLocalTime().Date;
		var lastDate   = DateTime.UnixEpoch.AddSeconds( int.Parse( fields[ "LastDate" ] ) ).ToLocalTime().Date;
		
		var lastDateOfSet  = DateHelper.Max( firstDate, lastDate.AddDays( -7 ) );
		var firstDateOfSet = lastDateOfSet.AddDays( -30 );

		var days = loader.LoadFromFolder( SD_CARD_ROOT, firstDateOfSet, lastDateOfSet );
		Assert.IsNotNull( days );
		Assert.IsTrue( days.Count > 0 );
		Assert.IsTrue( days[ 0 ].ReportDate >= firstDateOfSet );
		Assert.IsTrue( days[ days.Count - 1 ].ReportDate <= lastDateOfSet );
	}

	//[TestMethod]
	public void PropertiesFileExistsAndCanBeParsed()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );

        if (!File.Exists(propertyFilePath))
        {
            Assert.Inconclusive($"Test path '{propertyFilePath}' does not exist.");
        }

        var fields = ReadKeyValueFile( propertyFilePath );

		Assert.AreEqual( fields[ "SerialNumber" ],     "P1192913945CE" );
		Assert.AreEqual( fields[ "ModelNumber" ],      "560P" );
		Assert.AreEqual( fields[ "ProductType" ],      "0x35" );
		Assert.AreEqual( fields[ "FirstDate" ],        "1404914403" );
		Assert.AreEqual( fields[ "LastDate" ],         "1431907200" );
		Assert.AreEqual( fields[ "PatientFolderNum" ], "8" );
		Assert.AreEqual( fields[ "PatientFileNum" ],   "430" );

		Assert.IsTrue( _modelToProductName.TryGetValue( fields[ "ModelNumber" ], out string? productName ) );
		Assert.AreEqual( "REMstar Auto (System One 60 Series)", productName );

		var machineInfo = new PRS1DataLoader().LoadMachineIdentificationInfo( SD_CARD_ROOT );
		Assert.IsNotNull( machineInfo );
		Assert.IsFalse( string.IsNullOrEmpty( machineInfo.ProductName ) );
		Assert.IsFalse( string.IsNullOrEmpty( machineInfo.ModelNumber ) );
		Assert.IsFalse( string.IsNullOrEmpty( machineInfo.SerialNumber ) );
	}

	//[TestMethod]
	public void PatientFolderExists()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );

        if (!File.Exists(propertyFilePath))
        {
            Assert.Inconclusive($"Test path '{propertyFilePath}' does not exist.");
        }

		var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );

		Assert.IsTrue( Directory.Exists( patientFolderPath ) );

		Assert.IsTrue( int.TryParse( fields[ "PatientFileNum" ], out int correctFileCount ) );
		Assert.AreEqual( 430, correctFileCount );

		var dataFiles = Directory.GetFiles( patientFolderPath, "*.00?" );
		Assert.AreEqual( dataFiles.Length, correctFileCount );
	}

	//[TestMethod]
	public void CanReadDataFileHeader()
	{
		var propertyFilePath = Path.Combine( SOURCE_FOLDER, "Properties.txt" );

        if (!File.Exists(propertyFilePath))
        {
            Assert.Inconclusive($"Test path '{propertyFilePath}' does not exist.");
        }

        var fields            = ReadKeyValueFile( propertyFilePath );
		var patientFolderPath = Path.Combine( SOURCE_FOLDER, $"p{fields[ "PatientFolderNum" ]}" );
		var dataFiles         = Directory.GetFiles( patientFolderPath, "*.00?" );

		var filename = dataFiles.FirstOrDefault( x => x.EndsWith( ".001" ) );
		Assert.IsNotNull( filename );

		using var file  = File.Open( filename, FileMode.Open );
		using var reader = new BinaryReader( file, Encoding.ASCII );

		Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

		var header = HeaderRecord.Read( reader );

		// For .001 files there should only be one chunk, so BlockLength should match file size. This
		// won't be true for .002 and .005 files which contain multiple chunks. 
		Assert.AreEqual( file.Length, header.BlockLength );
		
		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( int.Parse( fields[ "Family" ] ),                           header.Family );
		Assert.AreEqual( int.Parse( fields[ "FamilyVersion" ] ),                    header.FamilyVersion );
		Assert.AreEqual( int.Parse( fields[ "DataFormatVersion" ] ),                header.DataFormatVersion );
		Assert.AreEqual( new DateTime( 2015, 4, 12 ),                               header.Timestamp.Date );
		Assert.AreEqual( 1,                                                         header.FileExtension );
		Assert.AreEqual( int.Parse( Path.GetFileNameWithoutExtension( filename ) ), header.SessionNumber );
	}

	//[TestMethod]
	public void CanReadSummaryFileChunks()
	{
        if (!File.Exists(SOURCE_FOLDER))
        {
            Assert.Inconclusive($"Test path '{SOURCE_FOLDER}' does not exist.");
        }

        var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.001", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );

				var settings = chunk.ReadSummary( chunk.Header );
				Assert.IsNotNull( settings );
				Assert.IsTrue( settings.Count > 0 );
			}
		}
	}

	//[TestMethod]
	public void CanSerializeAndDeserializeSettings()
	{
        if (!File.Exists(SOURCE_FOLDER))
        {
            Assert.Inconclusive($"Test path '{SOURCE_FOLDER}' does not exist.");
        }

        var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.001", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );

				var settings = chunk.ReadSummary( chunk.Header );

				var blob = new SettingDictionaryBlobConverter().ConvertToBlob( settings );
				Assert.IsNotNull( blob );
				Assert.IsInstanceOfType<byte[]>( blob );
				Assert.IsTrue( blob.Length > 0 );
				
				var deserialized = new SettingDictionaryBlobConverter().ConvertFromBlob( blob ) as Dictionary<string, object>;
				Assert.IsNotNull( deserialized );
				Assert.AreEqual( settings.Count, deserialized.Count );

				foreach( var pair in settings )
				{
					// There's no good way to serialize/deserialize an enum without converting it to a string that
					// specifies the fully qualified name of the enumeration, and that's pretty inefficient in 
					// multiple ways, so they are simply serialized as integers. This isn't a concern for the 
					// ParsedSettings class, which will have typed generic functions for retrieval of values.
					if( settings[ pair.Key ] is Enum )
					{
						Assert.AreEqual( (int)settings[ pair.Key ], deserialized[ pair.Key ] );
					}
					else
					{
						Assert.AreEqual( settings[ pair.Key ], deserialized[ pair.Key ] );
					}
				}
			}
		}
	}

	//[TestMethod]
	public void CanReadEventFileChunks()
	{
        if (!File.Exists(SOURCE_FOLDER))
        {
            Assert.Inconclusive($"Test path '{SOURCE_FOLDER}' does not exist.");
        }

        var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.002", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );

				var data = chunk.ReadEvents( chunk.Header );
				
				Assert.IsNotNull( data );
				Assert.IsNotNull( data.Events );
				Assert.IsNotNull( data.Stats );
				
				Assert.IsTrue( data.Stats.Count > 0 );
				Assert.IsTrue( data.Stats.Any( x => x.Name == SignalNames.Pressure ) );
				Assert.IsTrue( data.Stats.Any( x => x.Name == SignalNames.LeakRate ) );
				Assert.IsTrue( data.Stats.Any( x => x.Name == SignalNames.Snore ) );
			}
		}
	}

	//[TestMethod]
	public void CanReadWaveformFileChunks()
	{
        if (!File.Exists(SOURCE_FOLDER))
        {
            Assert.Inconclusive($"Test path '{SOURCE_FOLDER}' does not exist.");
        }

        var dataFiles = Directory.GetFiles( SOURCE_FOLDER, "*.005", SearchOption.AllDirectories );

		foreach( var filename in dataFiles )
		{
			using var file   = File.Open( filename, FileMode.Open );
			using var reader = new BinaryReader( file, Encoding.ASCII );

			Assert.IsTrue( file.Length >= 15, "Header records are supposed to be 15 bytes in length" );

			DataChunk? lastChunk = null;

			var chunks = new List<DataChunk>();
			
			while( file.Position < file.Length )
			{
				var chunk = DataChunk.Read( reader );
				Assert.IsNotNull( chunk );
				Assert.IsTrue( chunk.Header.Duration.TotalSeconds > 0 );
				Assert.IsNotNull( chunk.Header.SignalInfo );
				Assert.IsTrue( chunk.Header.SignalInfo.IntervalCount > 0 );
				
				if( lastChunk != null )
				{
					Assert.AreEqual( lastChunk.Header.HeaderType,                  chunk.Header.HeaderType );
					Assert.AreEqual( lastChunk.Header.SessionNumber,               chunk.Header.SessionNumber );
					Assert.AreEqual( lastChunk.Header.Family,                      chunk.Header.Family );
					Assert.AreEqual( lastChunk.Header.FamilyVersion,               chunk.Header.FamilyVersion );
					Assert.AreEqual( lastChunk.Header.DataFormatVersion,           chunk.Header.DataFormatVersion );
					Assert.AreEqual( lastChunk.Header.SignalInfo!.Waveforms.Count, chunk.Header.SignalInfo.Waveforms.Count );

					// Normally all chunks would have timestamps that were in chronological order, but the first chunk
					// of Session 551 in the test data has a timestamp that is *wildly* incorrect, and doesn't correspond
					// to the Session in any way. Looking at the Signal data for that Chunk it does appear to perfectly 
					// match up to the following Chunk, so maybe the timestamp of the first chunk in a Session can be safely
					// replaced with the timestamp of the Session in order to simplify handling this once-off situation?
					//
					if( chunk.Header.SessionNumber != 551 )
					{
						Assert.IsTrue( chunk.Header.Timestamp > lastChunk.Header.Timestamp, "Chunks are not chronologically ordered" );
					}

					// There can be a gap between chunks, which seems to always be flagged with a "Breathing Not Detected" event 
					//
					// var lastChunkEnd = lastChunk.Header.Timestamp + lastChunk.Header.SignalInfo.Duration;
					// var gap          = (chunk.Header.Timestamp - lastChunkEnd);
					// Assert.IsTrue( gap.TotalSeconds <= 1.0 );
				}
				
				lastChunk = chunk;

				chunks.Add( chunk );
			}
			
			// NOTE: The sample data I have only contains a single Signal per Session, so that is all the code is 
			// currently designed to handle. The PRS1 data format is rather opaque and appears to be entirely 
			// undocumented, so it's doubtful at this point that this library will be extended to cover other
			// Philips Respironics models. 
			var signals = ReadSignals( chunks );
			Assert.IsNotNull( signals );
			Assert.IsTrue( signals.Count > 0 );
		}
	}

	public List<Signal> ReadSignals( List<DataChunk> chunks )
	{
		const double SAMPLE_FREQUENCY = 0.2;
		
		if( chunks.Count == 0 )
		{
			return new List<Signal>();
		}

		var samples    = new List<byte>();
		var firstChunk = chunks[ 0 ];
		var lastChunk  = chunks[ chunks.Count - 1 ];
		var duration   = lastChunk.Header.EndTimestamp - firstChunk.Header.Timestamp;
		var numSignals = firstChunk.Header.SignalInfo!.Waveforms.Count;

		Debug.Assert( numSignals == 1, "Unexpected number of signals" );
		
		DataChunk? previousChunk = null;
		foreach( var chunk in chunks )
		{
			// If there is a gap between chunks, then we need to fill it
			if( previousChunk != null )
			{
				var gapLength = (chunk.Header.Timestamp - previousChunk.Header.EndTimestamp).TotalSeconds;
				if( gapLength > SAMPLE_FREQUENCY )
				{
					for( int i = 0; i < gapLength / SAMPLE_FREQUENCY; i++ )
					{
						samples.Add( 0 );
					}
				}
				else if( gapLength < -1 )
				{
					// There is an extremely rare but annoying issue in Session 551 of the sample data where the 
					// timestamp of the first chunk is *wildly* incorrect. On the assumption that if it happened
					// once it can happen again, we'll try to work around it.
					// It might be better to just throw away this Session, though.  
					previousChunk.Header.Timestamp = chunk.Header.Timestamp - previousChunk.Header.Duration;

					// Recalculate the Session duration to match the new timestamps 
					duration = lastChunk.Header.EndTimestamp - firstChunk.Header.Timestamp;
					
					Console.WriteLine( $"Unexpected chunk overlap found in Session {chunk.Header.SessionNumber}" );
				}
				else if( gapLength < 0 )
				{
					// Assuming that a single second overlap can be safely ignored seems to be born out when 
					// looking at the Signal data and how it still seamlessly matches up between Chunks. 
					duration += TimeSpan.FromSeconds( -gapLength );
				}
			}

			Debug.Assert( chunk.BlockData != null, "chunk.BlockData != null" );
			samples.AddRange( chunk.BlockData );
			previousChunk = chunk;
		}

		// Calculate the sample rate. Note that the sample rate for this machine is extremely low compared to ResMed machines. 
		var sampleRate = duration.TotalSeconds / samples.Count;
		Assert.AreEqual( SAMPLE_FREQUENCY, sampleRate, 0.001 );
		
		var signal = new Signal
		{
			Name              = SignalNames.FlowRate,
			FrequencyInHz     = sampleRate,
			MinValue          = -120,
			MaxValue          = 150,
			UnitOfMeasurement = "L/min",
			StartTime         = firstChunk.Header.Timestamp,
			EndTime           = previousChunk.Header.EndTimestamp,
		};
		
		signal.Samples.AddRange( samples.Select( x => (double)(sbyte)x ) );
		
		return new List<Signal> { signal };
	}

	private static Dictionary<string, string> ReadKeyValueFile( string path, string separator = "=" )
	{
		var fields = new Dictionary<string, string>();

		using var input = File.OpenText( path );

		while( !input.EndOfStream )
		{
			var line = input.ReadLine();
			if( string.IsNullOrEmpty( line ) )
			{
				break;
			}

			var parts = line.Split( separator );
			Assert.AreEqual( 2, parts.Length );

			fields[ parts[ 0 ] ] = parts[ 1 ];
		}

		return fields;
	}

	public enum HeaderType
	{
		Standard = 0,
		Signal = 1,
		MAX_VALUE = 1,
	}

	public class DataChunk
	{
		private const double SCALE = 0.1;
		
		public HeaderRecord? Header    { get; set; }
		public byte[]?       BlockData { get; set; }
		public ushort        Checksum  { get; set; }

		public static DataChunk Read( BinaryReader reader )
		{
			var startPosition = (int)reader.BaseStream.Position;

			var header = HeaderRecord.Read( reader );

			var chunk = new DataChunk()
			{
				Header = header,
			};

			var checksumSize = header.DataFormatVersion == 3 ? 4 : 2;

			if( header.DataFormatVersion == 2 )
			{
				var headerSize        = (int)reader.BaseStream.Position - startPosition;
				var dataSize          = header.BlockLength - headerSize - checksumSize;
				var blockData         = reader.ReadBytes( dataSize );
				var blockChecksum     = reader.ReadUInt16();
				var calcBlockChecksum = CRC16.Calc( blockData, blockData.Length );

				if( calcBlockChecksum != blockChecksum )
				{
					throw new Exception( $"Block checksum mismatch. Expected: {calcBlockChecksum}, Actual: {blockChecksum}" );
				}

				chunk.BlockData = blockData;
				chunk.Checksum  = blockChecksum;
			}
			else 
			{
				// TODO: Obtain sample data for DataFormat==3 
				// The only sample data I have available to me uses Version==2, so instead of guessing
				// that I can get Version==3 correct we will just throw an exception instead
				throw new NotSupportedException( $"Data Format Version {header.DataFormatVersion} in session {header.SessionNumber} is not yet supported" );
			}

			return chunk;
		}
		
		public EventImportData ReadEvents( HeaderRecord? header )
		{
			var events     = new List<ReportedEvent>();
			var statistics = new List<ValueAtTime>();
			
			var timestamp = header.Timestamp;

			Debug.Assert( BlockData != null, nameof( BlockData ) + " != null" );
			using var reader = new BinaryReader( new MemoryStream( BlockData ) );

			while( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				var eventCode = reader.ReadByte();

				timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );

				switch( eventCode )
				{
					case 0x01:
						{
							// Pressure set
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.Pressure,
								Value     = reader.ReadByte() * 0.1,
							} );
						}
						break;
					case 0x02:
						{
							// Pressure set (bilevel)
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.Pressure,
								Value     = reader.ReadByte() * 0.1,
							} );
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.EPAP,
								Value     = reader.ReadByte() * 0.1,
							} );
						}
						break;
					case 0x03:
						// Change to Opti-Start pressure? Not imported
						reader.ReadByte();
						break;
					case 0x04:
						// Pressure pulse. Not imported.
						reader.ReadByte();
						break;
					case 0x05:
						{
							// RERA
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
							
							events.Add( new ReportedEvent
							{
								Type       = EventType.RERA,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = TimeSpan.Zero,
							} );
						}
						break;
					case 0x06:
						{
							// Obstructive Apnea
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
							
							events.Add( new ReportedEvent
							{
								Type       = EventType.ObstructiveApnea,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = TimeSpan.Zero,
							} );
						}
						break;
					case 0x07:
						{
							// Central Apnea
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
							
							events.Add( new ReportedEvent
							{
								Type       = EventType.ClearAirway,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = TimeSpan.Zero,
							} );
						}
						break;
					case 0x0A:
						{
							// Hypopnea Type 1?
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
							
							events.Add( new ReportedEvent
							{
								Type       = EventType.Hypopnea,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = TimeSpan.Zero,
							} );
						}
						break;
					case 0x0B:
						{
							// Hypopnea Type 2? Maybe "Hypopnea, with duration?"
							// Not convinced that the first parameter is a duration, but will graph it and see if it lines up.
							// TODO: Looks like these "Hypopnea type 2" events frequently (if not always) overlap with the other type of Hypopnea event. Find out what that means. 
							var duration  = TimeSpan.FromSeconds( reader.ReadByte() );
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
							
							events.Add( new ReportedEvent
							{
								Type       = EventType.Hypopnea,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = duration,
							} );
						}
						break;
					case 0x0C:
						{
							// Flow Limitation
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed;
													
							events.Add( new ReportedEvent
							{
								Type       = EventType.FlowLimitation,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = TimeSpan.Zero,
							} );
						}
						break;
					case 0x0D:
						// Vibratory Snore
						events.Add( new ReportedEvent
						{
							Type       = EventType.VibratorySnore,
							SourceType = SourceType.CPAP,
							StartTime  = timestamp,
							Duration   = TimeSpan.Zero,
						} );
						break;
					case 0x0E:
						{
							// Variable Breathing
							var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed - duration;
														
							events.Add( new ReportedEvent
							{
								Type       = EventType.VariableBreathing,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = duration,
							} );
						}
						break;
					case 0x0F:
						{
							// Periodic Breathing
							var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed - duration;
														
							events.Add( new ReportedEvent
							{
								Type       = EventType.PeriodicBreathing,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = duration,
							} );
						}
						break;
					case 0x10:
						{
							// Large Leak
							var duration  = TimeSpan.FromSeconds( reader.ReadUInt16() * 2 );
							var elapsed   = TimeSpan.FromSeconds( reader.ReadByte() );
							var startTime = timestamp - elapsed - duration;
																		
							events.Add( new ReportedEvent
							{
								Type       = EventType.LargeLeak,
								SourceType = SourceType.CPAP,
								StartTime  = startTime,
								Duration   = duration,
							} );
						}
						break;
					case 0x11:
						{
							// Statistics 
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.LeakRate,
								Value     = reader.ReadByte() * 0.1,
							} );
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.Snore,
								Value     = reader.ReadByte(),
							} );
							statistics.Add( new ValueAtTime
							{
								Timestamp = timestamp,
								Name      = SignalNames.Pressure,
								Value     = reader.ReadByte() * 0.1,
							} );
						}
						break;
					case 0x12:
						{
							// Appears to be related to snore statistics, but also doesn't appear
							// to be relevant to anything I care about. Not imported.
							reader.ReadUInt16();
						}
						break;
					default:
						throw new NotSupportedException( $"Unknown event code {eventCode} in session {header.SessionNumber}" );
				}
			}

			return new EventImportData()
			{
				Events = events,
				Stats = statistics,
			};
		}
		
		public ParsedSettings ReadSummary( HeaderRecord? header )
		{
			var timestamp = header.Timestamp;

			var settings = new ParsedSettings();
			var sessions = new List<Session>();

			DateTime? lastMaskOn = null;
			var       startTime  = timestamp;
			var       endTime    = timestamp;

			Debug.Assert( BlockData != null, nameof( BlockData ) + " != null" );
			using var reader = new BinaryReader( new MemoryStream( BlockData ) );
			
			while( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				var code = reader.ReadByte();
				
				var blockStartPosition = reader.BaseStream.Position;
				
				switch( code )
				{
					case 0x00:
                        // Equipment On
                        ReadSettings( reader, settings );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 24 );
						break;
					case 0x01:
						// Equipment Off
						timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
						endTime   =  timestamp;
						reader.Advance( 5 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 0x02:
						// Mask On
						Debug.Assert( lastMaskOn == null, "Mismatched MaskOn/MaskOff" );
						timestamp  += TimeSpan.FromSeconds( reader.ReadUInt16() );
						lastMaskOn =  timestamp;
						reader.Advance( 3 );
                        ReadHumidifierSettings( reader, settings );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 7 );
						break;
					case 0x03:
						// Mask Off
						Debug.Assert( lastMaskOn != null, "Mismatched MaskOn/MaskOff" );
						timestamp  += TimeSpan.FromSeconds( reader.ReadUInt16() );
						sessions.Add( new Session() { StartTime = lastMaskOn.Value, EndTime = timestamp, SourceType = SourceType.CPAP } );
						lastMaskOn =  null;
						reader.Advance( 34 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 36 );
						break;
					case 0x04:
						// Time elapsed? Time correction? Not sure. Not encountered in sample data
						timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 2 );
						break;
					case 0x05:
					case 0x06:
						// Nothing to do here? Not encountered in sample data.
						break;
					case 0x07:
						// Humidifier settings changed between one Session and another? Or in any
						// case they are stored/retrieved again only after a Session has started. 
						Debug.Assert( lastMaskOn != null );
						timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
                        ReadHumidifierSettings( reader, settings );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 4 );
						break;
					case 0x08:
						// Related to Cpap-Check mode? Not seen in sample data. 
						timestamp += TimeSpan.FromSeconds( reader.ReadUInt16() );
						reader.Advance( 9 );
						Debug.Assert( reader.BaseStream.Position - blockStartPosition == 11 );
						break;
					default:
						throw new NotSupportedException( $"Unexpected code ({code:x}) reading chunk data in session {header.SessionNumber}" );
				}
			}

			// In the sample data that I have there may be one or two Sessions per file, but never zero.
			Assert.IsTrue( sessions.Count > 0, "No Sessions found" );
			Assert.AreEqual( startTime, sessions[ 0 ].StartTime );
			
			// It's not very common, but the Equipment Off and final Mask Off times are not always equal.
			// Equipment Off is however still always equal to or later than final Mask Off. 
			Assert.IsTrue( endTime >= sessions[ ^1 ].EndTime );

			return settings;
		}
		
		private static void ReadSettings( BinaryReader reader, ParsedSettings settings )
		{
			// Unknown meaning for this byte
			reader.ReadByte();
			
			// ReSharper disable UnusedVariable
			var mode                = ReadOperatingMode( reader );
			var minPressure         = reader.ReadByte() * SCALE;
			var maxPressure         = reader.ReadByte() * SCALE;
			var pressure            = (minPressure > 0) ? minPressure : maxPressure; // Only valid for the CPAP modes
			var minPS               = reader.ReadByte() * SCALE;
			var maxPS               = reader.ReadByte() * SCALE;
			var startupMode         = reader.ReadByte();
			var rampTime            = reader.ReadByte();
			var rampPressure        = reader.ReadByte() * SCALE;
			var flexMode            = ReadFlexInfo( reader, mode );
			var humidifierSettings  = ReadHumidifierSettings( reader );
			var resistanceFlags     = reader.ReadByte();
			var maskResistanceLevel = (resistanceFlags >> 3) & 0x07;
			var maskResistanceLock  = (resistanceFlags & 0x40) != 0;
			var hoseDiameter        = (resistanceFlags & 0x01) != 0 ? 15 : 22;
			var tubeTempLock        = (resistanceFlags & 0x02) != 0;
			var unknown1            = reader.ReadByte();
			var generalFlags        = reader.ReadByte();
			var autoOnEnabled       = (generalFlags & 0x40) != 0;
			var autoOffEnabled      = (generalFlags & 0x10) != 0;
			var maskAlertEnabled    = (generalFlags & 0x04) != 0;
			var showAHIEnabled      = (generalFlags & 0x02) != 0;
			var unknown2            = reader.ReadByte();
			var autoTrialDuration   = reader.ReadByte();
			// ReSharper enable UnusedVariable

			settings[ SettingNames.Mode ]               = mode;
			settings[ SettingNames.MinPressure ]        = minPressure;
			settings[ SettingNames.MaxPressure ]        = maxPressure;
			settings[ SettingNames.Pressure ]           = pressure;
			settings[ SettingNames.MinPressureSupport ] = minPS;
			settings[ SettingNames.MaxPressureSupport ] = maxPS;
			settings[ SettingNames.RampTime ]           = rampTime;
			settings[ SettingNames.RampPressure ]       = rampPressure;
			settings[ SettingNames.FlexMode ]           = flexMode.Mode;
			settings[ SettingNames.FlexLock ]           = flexMode.Locked;
			settings[ SettingNames.FlexLevel ]          = flexMode.Level;
			settings[ SettingNames.HumidifierAttached ] = humidifierSettings.HumidifierPresent;
			settings[ SettingNames.HumidifierMode ]     = humidifierSettings.Mode;
			settings[ SettingNames.HumidityLevel ]      = humidifierSettings.HumidityLevel;
			settings[ SettingNames.MaskResist ]         = maskResistanceLevel;
			settings[ SettingNames.MaskResistLock ]     = maskResistanceLock;
			settings[ SettingNames.AutoOn ]             = autoOnEnabled;
			settings[ SettingNames.AutoOff ]            = autoOffEnabled;
			settings[ SettingNames.AlertMask ]          = maskAlertEnabled;
			settings[ SettingNames.ShowAHI ]            = showAHIEnabled;
			settings[ SettingNames.HoseDiameter ]       = hoseDiameter;

			if( mode == OperatingMode.AutoTrial )
			{
				settings[ SettingNames.AutoTrialDuration ] = autoTrialDuration;
			}

			if( humidifierSettings.Mode == HumidifierMode.HeatedTube )
			{
				settings[ SettingNames.TubeTemperature ] = humidifierSettings.TubeTemperature;
				settings[ SettingNames.TubeTempLocked ]  = tubeTempLock;
			}

			Debug.Assert( unknown1 == 1 );
			Debug.Assert( unknown2 == 0 );

			var reservedBytes = reader.ReadBytes( 7 );
			Debug.Assert( !reservedBytes.Any( x => x != 0 ) );
		}

		private static void ReadHumidifierSettings( BinaryReader reader, ParsedSettings settings )
		{
			var humidifierSettings = ReadHumidifierSettings( reader );
			
			settings[ SettingNames.HumidifierAttached ]     = humidifierSettings.HumidifierPresent;
			settings[ SettingNames.HumidifierMode ] = humidifierSettings.Mode;
			settings[ SettingNames.HumidityLevel ]  = humidifierSettings.HumidityLevel;
			
			if( humidifierSettings.Mode == HumidifierMode.HeatedTube )
			{
				settings[ SettingNames.TubeTemperature ] = humidifierSettings.TubeTemperature;
			}
		}
		
		private static HumidifierSettings ReadHumidifierSettings( BinaryReader reader )
		{
			var flags1 = reader.ReadByte();
			var flags2 = reader.ReadByte();

			var  humidityLevel     = flags1 & 0x07;
			var  tubeHumidityLevel = (flags1 >> 4) & 0x07;
			var  tubeTemp          = (flags1 >> 7) | ((flags2 & 0x03) << 1);
			bool noData            = (flags2 & 0x10) != 0;
			bool isAdaptive        = (flags2 & 0x04) != 0;
			bool heatedTubeEnabled = (flags2 & 0x08) != 0 && !isAdaptive;
			var  humidifierMode    = heatedTubeEnabled ? HumidifierMode.HeatedTube : HumidifierMode.Fixed;

			humidifierMode = isAdaptive ? HumidifierMode.Adaptive : humidifierMode;

			return new HumidifierSettings
			{
				HumidifierPresent = !noData,
				Mode              = humidifierMode,
				HumidityLevel     = humidifierMode == HumidifierMode.HeatedTube ? tubeHumidityLevel : humidityLevel,
				TubeTemperature   = tubeTemp,
			};
		}
		
		private static FlexSettings ReadFlexInfo( BinaryReader reader, OperatingMode operatingMode )
		{
			var flexFlags = reader.ReadByte();
			
			// Extract the mode flags 
			bool enabled    = (flexFlags & 0x80) != 0;
			bool locked     = (flexFlags & 0x40) != 0;
			bool plain_flex = (flexFlags & 0x20) != 0;
			bool risetime   = (flexFlags & 0x10) != 0;
			bool plusmode   = (flexFlags & 0x08) != 0;
			int  flexlevel  = flexFlags & 0x03;

			if( !enabled )
			{
				return new FlexSettings() { Mode = FlexMode.None };
			}
			
			FlexMode flexMode = FlexMode.Unknown;

			if( risetime )
			{
				flexMode = FlexMode.RiseTime;
			}
			else if( plain_flex )
			{
				flexMode = FlexMode.Flex;
			}
			else if( plusmode )
			{
				flexMode = operatingMode switch
				{
					OperatingMode.CPAP       => FlexMode.CFlexPlus,
					OperatingMode.CPAP_Check => FlexMode.CFlexPlus,
					OperatingMode.AutoCPAP   => FlexMode.AFlex,
					OperatingMode.AutoTrial  => FlexMode.AFlex,
					_                        => throw new NotSupportedException( $"Unexpected Flex mode {flexFlags}" )
				};
			}
			else
			{
				flexMode = operatingMode switch
				{
					OperatingMode.CPAP_Check  => FlexMode.CFlex,
					OperatingMode.CPAP        => FlexMode.CFlex,
					OperatingMode.AutoCPAP    => FlexMode.CFlex,
					OperatingMode.AutoTrial   => FlexMode.CFlex,
					OperatingMode.Bilevel     => FlexMode.BiFlex,
					OperatingMode.AutoBilevel => FlexMode.BiFlex,
					_                         => throw new ArgumentOutOfRangeException( nameof( operatingMode ), operatingMode, null )
				};
			}

			return new FlexSettings()
			{
				Mode   = flexMode,
				Locked = locked,
				Level  = flexlevel,
			};
		}

		private static OperatingMode ReadOperatingMode( BinaryReader reader )
		{
			var mode = reader.ReadByte();
			return mode switch
			{
				0x00 => OperatingMode.CPAP,
				0x20 => OperatingMode.Bilevel,
				0x40 => OperatingMode.AutoCPAP,
				0x60 => OperatingMode.AutoBilevel,
				0x80 => OperatingMode.AutoTrial,
				0xA0 => OperatingMode.CPAP_Check,
				_    => throw new NotSupportedException( $"Uknown Operating Mode value: {mode}" )
			};
		}
	}

	public class HeaderSignalInfo
	{
		public int IntervalCount  { get; set; }
		public int IntervalLength { get; set; }

		public List<WaveformInfo> Waveforms = new();
		
		public TimeSpan Duration
		{
			get => TimeSpan.FromSeconds( IntervalCount * IntervalLength );
		}

		public class WaveformInfo
		{
			public int SampleFormat { get; set; }
			public int Interleave   { get; set; }
		}
	}

	public class HeaderRecord
	{
		public HeaderType HeaderType        { get; set; }
		public int        DataFormatVersion { get; set; }
		public int        BlockLength       { get; set; }
		public int        Family            { get; set; }
		public int        FamilyVersion     { get; set; }
		public int        FileExtension     { get; set; }
		public int        SessionNumber     { get; set; }
		public DateTime   Timestamp         { get; set; }

		public HeaderSignalInfo? SignalInfo { get; set; }

		public DateTime EndTimestamp { get => Timestamp + Duration; } 
		
		public TimeSpan Duration
		{
			get
			{
				if( HeaderType == HeaderType.Signal && SignalInfo != null )
				{
					return SignalInfo.Duration;
				}
				
				return TimeSpan.Zero;
			}
		}

		public static HeaderRecord Read( BinaryReader reader )
		{
			var startPosition = reader.BaseStream.Position;

			HeaderRecord? header = null;

			var dataFormatVersion = reader.ReadByte();
			var blockLength       = reader.ReadUInt16();
			var headerType        = (HeaderType)reader.ReadByte();
			var family            = reader.ReadByte();
			var familyVersion     = reader.ReadByte();
			var fileExtension     = reader.ReadByte();
			var sessionNumber     = (int)reader.ReadUInt32();
			var timestampNum      = (int)reader.ReadUInt32();
			var timestamp         = DateTime.UnixEpoch.AddSeconds( timestampNum ).ToLocalTime();

			if( family != 0 || familyVersion != 4 )
			{
				throw new NotSupportedException( $"This data format is not yet supported: Family {family} Version {familyVersion}" );
			}

			header = new HeaderRecord
			{
				DataFormatVersion = dataFormatVersion,
				BlockLength       = blockLength,
				HeaderType        = headerType,
				Family            = family,
				FamilyVersion     = familyVersion,
				FileExtension     = fileExtension,
				SessionNumber     = sessionNumber,
				Timestamp         = timestamp
			};

			if( dataFormatVersion != 0x02 )
			{
				throw new NotSupportedException( $"Data format version {dataFormatVersion} in session {sessionNumber} is not yet supported." );
			}
			
			if( headerType == HeaderType.Signal )
			{
				var interleavedRecordCount  = reader.ReadUInt16();
				var interleavedRecordLength = reader.ReadByte();
				var signalCount             = reader.ReadByte();

				header.SignalInfo = new HeaderSignalInfo
				{
					IntervalCount  = interleavedRecordCount,
					IntervalLength = interleavedRecordLength,
				};

				for( int i = 0; i < signalCount; i++ )
				{
					var signalType = reader.ReadByte();
					var interleave = reader.ReadUInt16();

					header.SignalInfo.Waveforms.Add( new HeaderSignalInfo.WaveformInfo
					{
						SampleFormat = signalType,
						Interleave   = interleave,
					} );
				}
				
				// Read terminator byte
				var terminatorByte = reader.ReadByte();
				Debug.Assert( 0 == terminatorByte );
			}
			
			// Now that we know the full header size, rewind the stream and read all header
			// bytes as a single array so that we can validate the checksum. 
			// NOTE: This obviously means that the base stream must support random access 
			// and that if the underlying data source is encrypted or compressed, it must 
			// first be fully decrypted/decompressed before calling this function. 
			var headerSize = (int)(reader.BaseStream.Position - startPosition);
			reader.BaseStream.Position = startPosition;
			var headerBytes = reader.ReadBytes( headerSize );
			
			// Calculate and verify header checksum 
			var headerCheckSum     = reader.ReadByte();
			var calcHeaderChecksum = Checksum8.Calc( headerBytes );
			if( calcHeaderChecksum != headerCheckSum )
			{
				throw new Exception( $"Header checksum mismatch. Expected: {calcHeaderChecksum}, Actual: {headerCheckSum}" );
			}

			return header;
		}
	}

	public class ParsedSettings : Dictionary<string, object>
	{
		
	}

	private class HumidifierSettings
	{
		public bool           HumidifierPresent { get; set; }
		public HumidifierMode Mode              { get; set; }
		public int            HumidityLevel     { get; set; }
		public double         TubeTemperature   { get; set; }
	}

	private enum HumidifierMode
	{
		Fixed, 
		Adaptive, 
		HeatedTube, 
		Passover, 
		Error,
	}

	private class FlexSettings
	{
		public FlexMode Mode   { get; set; }
		public bool     Locked { get; set; }
		public int      Level  { get; set; }
	}

	private enum FlexMode
	{
		Unknown = -1,
		None, 
		CFlex,
		CFlexPlus,
		AFlex,
		RiseTime, 
		BiFlex,
		PFlex, 
		Flex, 
	};

	private enum OperatingMode
	{
		UNKNOWN    = -1,
		CPAP_Check = 0,
		CPAP,
		AutoCPAP,
		AutoTrial,
		Bilevel,
		AutoBilevel,
		ASV,
		S,
		ST,
		PC,
		ST_AVAPS,
		PC_AVAPS,
	};

	public class ValueAtTime
	{
		public required DateTime Timestamp { get; set; }
		public required string   Name      { get; set; }
		public required double   Value     { get; set; }

		public override string ToString()
		{
			return $"{Name} = {Value:F2}    {Timestamp}";
		}
	}

	public class EventImportData
	{
		public List<ReportedEvent> Events { get; init; } = new List<ReportedEvent>();
		public List<ValueAtTime>   Stats  { get; init; } = new List<ValueAtTime>();
	}
}

public static class BinaryReaderExtensions
{
	public static void Advance( this BinaryReader reader, int count )
	{
		if( reader.BaseStream.Position + count > reader.BaseStream.Length )
		{
			throw new EndOfStreamException();
		}
			
		reader.BaseStream.Position += count;
	}
}
