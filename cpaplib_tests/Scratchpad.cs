using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

using cpap_app.Converters;

using cpaplib;

using StagPoint.EDF.Net;

namespace cpaplib_tests;

[TestClass]
public class Scratchpad
{
    [TestMethod]
    public void CanDetectDaylightSavingTime()
    {
        DateTime testDateTime = new(2024, 5, 15, 14, 31, 0, DateTimeKind.Local);
        var isDaylightSavings = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time").IsDaylightSavingTime(testDateTime);
        Assert.IsTrue( isDaylightSavings, $"Date '{testDateTime}' ({testDateTime.Kind}), isDaylightSavings = {isDaylightSavings}." );

        testDateTime = new DateTime(2024, 2, 15, 14, 31, 0, DateTimeKind.Local);
        isDaylightSavings = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time").IsDaylightSavingTime(testDateTime);
        Assert.IsFalse( isDaylightSavings, $"Date '{testDateTime}' ({testDateTime.Kind}), isDaylightSavings = {isDaylightSavings}.");
    }

    [TestMethod]
    public void CanSerializeAndDeserializeNumberDictionary()
    {
        var settingNames = GetAllPublicConstantValues<string>( typeof( SettingNames ) );
        var dict         = new Dictionary<string, double>( settingNames.Count );

        for( int i = 0; i < settingNames.Count; i++ )
        {
            dict[ settingNames[ i ] ] = i;
        }

        var serialized = new NumberDictionaryBlobConverter().ConvertToBlob( dict );
        Assert.IsNotNull( serialized );

        var deserialized = new NumberDictionaryBlobConverter().ConvertFromBlob( serialized ) as Dictionary<string, double>;
        Assert.IsNotNull( deserialized );

        for( int i = 0; i < deserialized.Count; i++ )
        {
            Assert.AreEqual( i, deserialized[ settingNames[ i ] ] );
        }
    }

    public static List<T> GetAllPublicConstantValues<T>( Type type )
    {
        return type
            .GetTypeInfo()
            .GetFields( BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy )
            .Where( fi => fi is { IsLiteral: true, IsInitOnly: false } && fi.FieldType == typeof( T ) )
            .Select( x => (T)x.GetRawConstantValue()! ?? default( T ) )
            .ToList()!;
    }

    [TestMethod]
    public void DateTimeFromGoogleAPI()
    {
        var startTime = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds( 1700028990000 );
        var endTime   = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds( 1700055270000 );

        var compareEndTime = new DateTime( 1970, 1, 1 ).ToLocalTime().AddMilliseconds( 1700055270000 );

        Assert.AreEqual( endTime, compareEndTime );

        Debug.WriteLine( $"Start: {startTime},    End: {endTime},    Duration: {endTime - startTime}" );
    }

    //[TestMethod]
    public void ReadViatomBinaryFile()
    {
        const int HEADER_SIZE = 40;
        const int RECORD_SIZE = 5;

        string path = @"D:\Data Files\Viatom\23072C0009\20231004031747";

        if (!File.Exists( path ))
        {
            Assert.Inconclusive($"Test path '{path}' does not exist.");
        }

        using var file   = File.OpenRead( path );
        using var reader = new BinaryReader( file );

        int fileVersion = reader.ReadInt16();
        Assert.IsTrue( fileVersion is 3 or 5 ); // Never actually encountered FileVersion=5, but apparently someone else has and it's binary compatible

        int year = reader.ReadInt16();
        Assert.AreEqual( 2023, year );

        int month = reader.ReadByte();
        Assert.IsTrue( month >= 1 && month <= 12 );

        int day = reader.ReadByte();
        Assert.IsTrue( day is >= 1 and <= 31 );

        int hour = reader.ReadByte();
        Assert.IsTrue( hour is >= 0 and <= 24 );

        int minute = reader.ReadByte();
        Assert.IsTrue( minute is >= 0 and <= 60 );

        int second = reader.ReadByte();
        Assert.IsTrue( second is >= 0 and <= 60 );

        var expectedTimestamp = DateTime.Parse( "2023-10-04 03:17:47.000" );
        var headerTimestamp   = new DateTime( year, month, day, hour, minute, second );
        Assert.AreEqual( expectedTimestamp, headerTimestamp );

        // Read timestamp (NOTE: The filename also appears to be a timestamp. Do they always match?)
        var filenameTimestamp = DateTime.ParseExact( Path.GetFileName( path ), "yyyyMMddHHmmsss", CultureInfo.InvariantCulture );
        Assert.AreEqual( expectedTimestamp, filenameTimestamp );

        // Read and validate file size 
        int fileSize = reader.ReadInt32();
        Assert.AreEqual( file.Length, fileSize );

        // Read duration of recording 
        var duration = TimeSpan.FromSeconds( reader.ReadInt16() );
        Assert.AreEqual( TimeSpan.FromSeconds( 5316 ), duration );

        // Skip the rest of the header, as it doesn't provide any useful information for us. 
        file.Position = HEADER_SIZE;

        // The rest of the file should be an exact multiple of RECORD_SIZE
        Assert.AreEqual( 0, (fileSize - HEADER_SIZE) % RECORD_SIZE );

        int    recordCount = (fileSize - HEADER_SIZE) / RECORD_SIZE;
        double frequency   = 1.0 / (duration.TotalSeconds / recordCount);

        for( int i = 0; i < recordCount; i++ )
        {
            var spO2      = reader.ReadByte();
            var pulse     = reader.ReadByte();
            var isInvalid = reader.ReadByte();
            var motion    = reader.ReadByte();
            var vibration = reader.ReadByte();

            Debug.WriteLine( $"OX: {spO2}, PULSE: {pulse}, MOT: {motion}, INVALID: {isInvalid}, VIB: {vibration}" );
        }
    }

    [TestMethod]
    public void BinaryHeapForStatCalculation()
    {
        var signal = new Signal
        {
            Name              = "TestSignal",
            FrequencyInHz     = 1,
            UnitOfMeasurement = "px",
            StartTime         = DateTime.MinValue,
            EndTime           = DateTime.MaxValue
        };

        const int COUNT = 1000;

        for( int i = 0; i < COUNT; i++ )
        {
            signal.Samples.Add( i );
        }

        var calculator = new SignalStatCalculator();
        calculator.AddSignal( signal );

        var stats = calculator.CalculateStats();

        Assert.AreEqual( 1,             stats.Minimum ); // "Minimum" for the stats calculator means "minimum value above zero"
        Assert.AreEqual( COUNT - 1,     stats.Maximum );
        Assert.AreEqual( COUNT * 0.95,  stats.Percentile95 );
        Assert.AreEqual( COUNT * 0.995, stats.Percentile995 );
    }
}