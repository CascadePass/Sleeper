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