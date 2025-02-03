using cpaplib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace cpaplib_tests
{
    [TestClass]
    public class ResMedDataLoaderTests
    {
        #region GetOperatingMode

        [TestMethod]
        public void GetOperatingMode_SampleDocument()
        {
            var result = ResMedDataLoader.GetOperatingMode(this.GetSampleData_AS11());

            Assert.AreEqual(OperatingMode.Apap, result);
        }

        #region Sanity Checks

        [TestMethod]
        public void GetOperatingMode_null()
        {
            var result = ResMedDataLoader.GetOperatingMode(null);
            Assert.AreEqual(OperatingMode.UNKNOWN, result);
        }

        [TestMethod]
        public void GetOperatingMode_EmptyDictionary()
        {
            var result = ResMedDataLoader.GetOperatingMode(new());
            Assert.AreEqual(OperatingMode.UNKNOWN, result);
        }

        [TestMethod]
        public void GetOperatingMode_MissingKeys()
        {
            Dictionary<string, double> data = this.GetSampleData_AS11();

            data.Remove("Mode");
            data.Remove("CPAP_MODE");

            var result = ResMedDataLoader.GetOperatingMode(data);
            Assert.AreEqual(OperatingMode.UNKNOWN, result);
        }

        #endregion

        #region LegacyMode

        [TestMethod]
        public void GetOperatingMode_LegacyMode_1()
        {
            Dictionary<string, double> testData = new()
            {
                { "CPAP_MODE", 1 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Apap, result);
        }

        [TestMethod]
        public void GetOperatingMode_LegacyMode_2()
        {
            Dictionary<string, double> testData = new()
            {
                { "CPAP_MODE", 2 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Apap, result);
        }

        [TestMethod]
        public void GetOperatingMode_LegacyMode_3()
        {
            Dictionary<string, double> testData = new()
            {
                { "CPAP_MODE", 3 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Cpap, result);
        }

        [TestMethod]
        public void GetOperatingMode_LegacyMode_Unknown()
        {
            Dictionary<string, double> testData = new()
            {
                { "CPAP_MODE", 4 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.UNKNOWN, result);

            testData["CPAP_MODE"] = -1;
            result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.UNKNOWN, result);

            testData["CPAP_MODE"] = 900;
            result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.UNKNOWN, result);
        }

        #endregion

        #region Mode

        [TestMethod]
        public void GetOperatingMode_Mode_0()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 0 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Cpap, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_1()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 1 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Apap, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_2()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 2 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.BilevelFixed, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_3()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 3 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.BilevelFixed, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_4()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 4 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.BilevelFixed, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_5()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 5 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.BilevelFixed, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_6()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 6 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.BilevelAutoFixedPS, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_7()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 7 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Asv, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_8()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 8 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.AsvVariableEpap, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_9()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 9 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Avaps, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_11()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 11 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Apap, result);
        }

        [TestMethod]
        public void GetOperatingMode_Mode_UnknownValues()
        {
            Dictionary<string, double> testData = new()
            {
                { "Mode", 10 }
            };

            var result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Cpap, result);

            testData["Mode"] = -1;
            result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Cpap, result);

            testData["Mode"] = int.MaxValue;
            result = ResMedDataLoader.GetOperatingMode(testData);
            Assert.AreEqual(OperatingMode.Cpap, result);
        }

        #endregion

        #endregion

        #region ReadMachineSettings

        [TestMethod]
        public void ReadMachineSettings_APAP_MinMaxPressure()
        {
            var data = new Dictionary<string, double>
            {
                { "CPAP_MODE", 1 },
                { "S.A.MinPress", 5.0 },
                { "S.A.MaxPress", 15.0 },
                { "S.C.Press", 10.0 },
                { "S.RampEnable", 1.0 },
                { "S.C.StartPress", 4.0 },
                { "S.RampTime", 20.0 },
                { "S.SmartStart", 1.0 },
                { "S.ABFilter", 1.0 },
                { "HeatedTube", 1.0 },
                { "Humidifier", 1.0 },
                { "S.ClimateControl", 1.0 },
                { "S.HumEnable", 1.0 },
                { "S.HumLevel", 3.0 },
                { "S.TempEnable", 1.0 },
                { "S.Temp", 25.0 },
                { "S.Mask", 1.0 },
                { "S.PtAccess", 1.0 },
            };

            var settings = ResMedDataLoader.ReadMachineSettings(data);

            Assert.AreEqual(OperatingMode.Apap, settings[SettingNames.Mode]);
            Assert.AreEqual(5.0, settings[SettingNames.MinPressure]);
            Assert.AreEqual(15.0, settings[SettingNames.MaxPressure]);
        }

        [TestMethod]
        public void ReadMachineSettings_UnsupportedMode_ThrowsNotSupportedException()
        {
            var data = new Dictionary<string, double>
            {
                { "S.C.Press", 10.0 },
                { "S.RampEnable", 1.0 },
                { "S.C.StartPress", 4.0 },
                { "S.RampTime", 20.0 },
                { "S.SmartStart", 1.0 },
                { "S.ABFilter", 1.0 },
                { "HeatedTube", 1.0 },
                { "Humidifier", 1.0 },
                { "S.ClimateControl", 1.0 },
                { "S.HumEnable", 1.0 },
                { "S.HumLevel", 3.0 },
                { "S.TempEnable", 1.0 },
                { "S.Temp", 25.0 },
                { "S.Mask", 1.0 },
                { "S.PtAccess", 1.0 }
            };

            Assert.ThrowsException<NotSupportedException>(() => ResMedDataLoader.ReadMachineSettings(data));
        }

        [TestMethod]
        public void ReadMachineSettings_EprEnabled_SetsEprSettings()
        {
            var data = new Dictionary<string, double>
            {
                { "CPAP_MODE", 1 },
                { "S.EPR.EPREnable", 1.0 },
                { "S.EPR.Level", 2.0 },
                { "S.EPR.EPRType", 1.0 },
                { "S.C.Press", 10.0 },
                { "S.RampEnable", 1.0 },
                { "S.C.StartPress", 4.0 },
                { "S.RampTime", 20.0 },
                { "S.SmartStart", 1.0 },
                { "S.ABFilter", 1.0 },
                { "HeatedTube", 1.0 },
                { "Humidifier", 1.0 },
                { "S.ClimateControl", 1.0 },
                { "S.HumEnable", 1.0 },
                { "S.HumLevel", 3.0 },
                { "S.TempEnable", 1.0 },
                { "S.Temp", 25.0 },
                { "S.Mask", 1.0 },
                { "S.PtAccess", 1.0 },
                { "S.A.MinPress", 5.0 },
                { "S.A.MaxPress", 15.0 },
            };

            var settings = ResMedDataLoader.ReadMachineSettings(data);

            Assert.IsTrue(settings.GetValue<bool>(SettingNames.EprEnabled));
            Assert.AreEqual(2, settings[SettingNames.EprLevel]);
            Assert.AreEqual((EprType)2, settings[SettingNames.EprMode]);
        }

        #endregion

        #region HasCorrectFolderStructure

        [TestMethod]
        public void HasCorrectFolderStructure_Root()
        {
            ResMedDataLoader resMedDataLoader = new();
            string testFolder = "C:\\";

            Assert.IsFalse(resMedDataLoader.HasCorrectFolderStructure(testFolder));
        }

        [TestMethod]
        public void HasCorrectFolderStructure_Root_ThrowIfNot()
        {
            ResMedDataLoader resMedDataLoader = new();
            string testFolder = "C:\\";

            Assert.ThrowsException<FileNotFoundException>(() => resMedDataLoader.HasCorrectFolderStructure(testFolder, true));

        }

        [TestMethod]
        public void EnsureCorrectFolderStructure_Root()
        {
            ResMedDataLoader resMedDataLoader = new();
            string testFolder = "C:\\";

            Assert.ThrowsException<FileNotFoundException>(() => resMedDataLoader.HasCorrectFolderStructure(testFolder, true));

        }

        #endregion

        private Dictionary<string, double> GetSampleData_AS11()
        {
            Dictionary<string, double> data = new()
            {
                { "Date", 19954 },
                { "MaskOn", 497 },
                { "MaskOff", 497 },
                { "MaskEvents", 14 },
                { "Duration", 528 },
                { "Mode", 1 },
                { "S.C.StartPress", 4 },
                { "S.C.Press", 10 },
                { "S.A.StartPress", 4 },
                { "S.A.MaxPress", 20 },
                { "S.A.MinPress", 4 },
                { "S.AFH.StartPress", 4 },
                { "S.AFH.MaxPress", 20 },
                { "S.AFH.MinPress", 4 },
                { "S.AS.Comfort", 1 },
                { "S.RampEnable", 3 },
                { "S.RampTime", 20 },
                { "S.EPR.ClinEnable", 2 },
                { "S.EPR.EPREnable", 2 },
                { "S.EPR.Level", 1 },
                { "S.EPR.EPRType", 2 },
                { "S.SmartStart", 2 },
                { "S.PtAccess", 1 },
                { "S.ABFilter", 1 },
                { "S.Mask", 3 },
                { "S.Tube", 3 },
                { "S.ClimateControl", 1 },
                { "S.HumEnable", 2 },
                { "S.HumLevel", 4 },
                { "S.TempEnable", 3 },
                { "S.Temp", 27 },
                { "HeatedTube", 1 },
                { "Humidifier", 2 },
                { "BlowPress.95", 10.24 },
                { "BlowPress.5", 4.3 },
                { "Flow.95", 0.92 },
                { "Flow.5", 0.12 },
                { "BlowFlow.50", 0.5760000000000001 },
                { "AmbHumidity.50", 11.600000000000001 },
                { "HumTemp.50", 46 },
                { "HTubeTemp.50", 27 },
                { "HTubePow.50", 14.8 },
                { "HumPow.50", 34.4 },
                { "SpO2.50", -1 },
                { "SpO2.95", -1 },
                { "SpO2.Max", -1 },
                { "SpO2Thresh", -1 },
                { "MaskPress.50", 4.16 },
                { "MaskPress.95", 8.96 },
                { "MaskPress.Max", 10.24 },
                { "TgtIPAP.50", 4.6000000000000005 },
                { "TgtIPAP.95", 9.8 },
                { "TgtIPAP.Max", 10.8 },
                { "TgtEPAP.50", 3.96 },
                { "TgtEPAP.95", 8.76 },
                { "TgtEPAP.Max", 9.96 },
                { "Leak.50", 0.14 },
                { "Leak.95", 0.28 },
                { "Leak.70", 0.16 },
                { "Leak.Max", 0.38 },
                { "MinVent.50", 6.875 },
                { "MinVent.95", 16.75 },
                { "MinVent.Max", 24.75 },
                { "RespRate.50", 13.600000000000001 },
                { "RespRate.95", 19.6 },
                { "RespRate.Max", 25.200000000000003 },
                { "TidVol.50", 0.46 },
                { "TidVol.95", 1.36 },
                { "TidVol.Max", 1.8 },
                { "AHI", 3.5 },
                { "HI", 0.4 },
                { "AI", 3 },
                { "OAI", 1.9000000000000001 },
                { "CAI", 1.1 },
                { "UAI", 0 },
                { "RIN", 0.2 },
                { "CSR", 0 },
                { "Crc16", 10605 }
            };

            return data;
        }
    }
}
