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