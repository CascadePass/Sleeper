using System;

namespace cpaplib
{
    /*

    {
  "FlowGenerator": {
    "IdentificationProfiles": {
      "Product": {
        "UniversalIdentifier": "aac1e42a-1111-48d3-918c-517bb1fc7f3d",
        "SerialNumber": "23241871111",
        "SerialNumberVerificationCode": "",
        "ProductCode": "39517",
        "ProductName": "AirSense 11 AutoSet",
        "FdaUniqueDeviceIdentifier": "",
        "ProductGeographicIdentifier": "USA"
      },
      "Hardware": { "HardwareIdentifier": "(90)R390-1111(91)1S003(21)3241R04978" },
      "Software": {
        "BootloaderIdentifier": "SW04601.00.1.1.0.736edbdfd",
        "ApplicationIdentifier": "SW04600.11.8.0.0.a65babae1",
        "ConfigurationIdentifier": "CF04600.11.03.00.a65babae1",
        "PlatformIdentifier": 46,
        "VariantIdentifier": 3,
        "RegionIdentifier": 0,
        "ProfileVariationIdentifier": "00000000-0000-3000-8000-000011046003",
        "DataVersionIdentifier": 11,
        "DataModelVersionIdentifier": "2.3.0.46f0081fc"
      }
    }
  }
}
     
     */

    /// <summary>
    /// Nested class structure used to parse the Identification.json file.
    /// </summary>
    public class JsonMachineIdentificationFile
    {
        /// <summary>
        /// Gets all of the data from the Identification.json file at the top level.
        /// </summary>
        public JsonIdentificationFileWrapperElement FlowGenerator { get; set; }

        public override string ToString() => this.FlowGenerator?.IdentificationProfiles?.Product?.ProductName;

        /// <summary>
        /// Class modeling the IdentificationProfiles in an Identification.json file.
        /// </summary>
        public class JsonIdentificationProfiles
        {
            public JsonIdentificationFileProduct Product { get; set; }
            public JsonIdentificationFileHardware Hardware { get; set; }
            public JsonIdentificationFileSoftware Software { get; set; }
        }

        /// <summary>
        /// Class modeling the product details in an Identification.json file.
        /// </summary>
        public class JsonIdentificationFileProduct
        {
            public Guid UniversalIdentifier { get; set; }
            public string SerialNumber { get; set; }
            public string SerialNumberVerificationCode { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public string FdaUniqueDeviceIdentifier { get; set; }
            public string ProductGeographicIdentifier { get; set; }
        }

        /// <summary>
        /// Class modeling the hardware details in an Identification.json file.
        /// </summary>
        public class JsonIdentificationFileHardware
        {
            public string HardwareIdentifier { get; set; }
        }

        /// <summary>
        /// Class modeling the software details in an Identification.json file.
        /// </summary>
        public class JsonIdentificationFileSoftware
        {
            public string BootloaderIdentifier { get; set; }
            public string ApplicationIdentifier { get; set; }
            public string ConfigurationIdentifier { get; set; }
            public int PlatformIdentifier { get; set; }
            public int VariantIdentifier { get; set; }
            public int RegionIdentifier { get; set; }
            public string ProfileVariationIdentifier { get; set; }
            public int DataVersionIdentifier { get; set; }
            public string DataModelVersionIdentifier { get; set; }
        }

        /// <summary>
        /// Class modeling the FlowGenerator object in an Identification.json file.
        /// </summary>
        public class JsonIdentificationFileWrapperElement
        {
            public JsonIdentificationProfiles IdentificationProfiles { get; set; }
        }
    }
}
