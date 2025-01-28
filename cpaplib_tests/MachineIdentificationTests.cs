using cpaplib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpaplib_tests
{
    [TestClass]
    public class MachineIdentificationTests
    {
        #region File Data

        private const string RESMED_11_IDENTIFICATION_FILE = @"{
  ""FlowGenerator"": {
    ""IdentificationProfiles"": {
      ""Product"": {
        ""UniversalIdentifier"": ""aac1e42a-1111-48d3-918c-517bb1fc7f3d"",
        ""SerialNumber"": ""23241871111"",
        ""SerialNumberVerificationCode"": """",
        ""ProductCode"": ""39517"",
        ""ProductName"": ""AirSense 11 AutoSet"",
        ""FdaUniqueDeviceIdentifier"": """",
        ""ProductGeographicIdentifier"": ""USA""
      },
      ""Hardware"": { ""HardwareIdentifier"": ""(90)R390-1111(91)1S003(21)3241R04978"" },
      ""Software"": {
        ""BootloaderIdentifier"": ""SW04601.00.1.1.0.736edbdfd"",
        ""ApplicationIdentifier"": ""SW04600.11.8.0.0.a65babae1"",
        ""ConfigurationIdentifier"": ""CF04600.11.03.00.a65babae1"",
        ""PlatformIdentifier"": 46,
        ""VariantIdentifier"": 3,
        ""RegionIdentifier"": 0,
        ""ProfileVariationIdentifier"": ""00000000-0000-3000-8000-000011046003"",
        ""DataVersionIdentifier"": 11,
        ""DataModelVersionIdentifier"": ""2.3.0.46f0081fc""
      }
    }
  }
}";

        private const string RESMED_10_IDENTIFICATION_FILE = @"
#IMF 0000

#VIR 0067

#RIR 0064

#PVR 0065

#PVD 001A

#CID CX036-009-036-026-103-100-101

#RID 0024

#VID 0009

#SRN 23222849021

#SID SX567-0401

#PNA AirCurve_10_VAuto

#PCD 37143

#PCB (90)R370-7518(91)T1(21)28032816

#MID 0024

#FGT 24_M36_V9

#BID SX577-0200
";

        #endregion

        [TestMethod]
        public void CanReadResmed11_IdentificationJson()
        {
            ResMedDataLoader resMedDataLoader = new();

            var machineID = resMedDataLoader.GetMachineIdentificationFromJson(RESMED_11_IDENTIFICATION_FILE);

            // Must be a result
            Assert.IsNotNull(machineID);

            // Validate the data (properties have correct values)
            Assert.AreEqual("AirSense 11 AutoSet", machineID.ProductName);
            Assert.AreEqual("23241871111", machineID.SerialNumber);
            Assert.AreEqual("39517", machineID.ModelNumber);
            Assert.AreEqual(MachineManufacturer.ResMed, machineID.Manufacturer);

            // Validate the ToString() method
            Assert.AreEqual("AirSense 11 AutoSet (SN: 23241871111)", machineID.ToString());

            // Validate the return type (of resMedDataLoader.GetMachineIdentificationFromJson)
            Assert.IsInstanceOfType<MachineIdentification>(machineID);
        }

        [TestMethod]
        public void CanReadResmed10_IdentificationFile()
        {
            MemoryStream memoryStream = new();
            StreamWriter streamWriter = new(memoryStream);

            streamWriter.Write(RESMED_10_IDENTIFICATION_FILE);
            streamWriter.Flush();

            memoryStream.Position = 0;

            ResMedDataLoader resMedDataLoader = new();
            var machineID = resMedDataLoader.GetMachineIdentificationFromStream(memoryStream);

            // Must be a result
            Assert.IsNotNull(machineID);

            // Validate the data (properties have correct values)
            Assert.AreEqual("AirCurve 10 VAuto", machineID.ProductName);
            Assert.AreEqual("23222849021", machineID.SerialNumber);
            Assert.AreEqual("37143", machineID.ModelNumber);
            Assert.AreEqual(MachineManufacturer.ResMed, machineID.Manufacturer);
        }
    }
}
