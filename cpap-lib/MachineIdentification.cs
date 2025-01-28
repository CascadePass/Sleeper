using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace cpaplib
{
    public class MachineIdentification
	{
		#region Public properties

		/// <summary>
		/// Returns the name of the manufacturer of the xPAP machine. Set by the importer.
		/// </summary>
		public MachineManufacturer Manufacturer { get; set; } = MachineManufacturer.ResMed;

		/// <summary>
		/// The Product Name of the machine, as reported
		/// </summary>
		public string ProductName { get; set; } = "UNKNOWN";

		/// <summary>
		/// The machine's Serial Number, as reported
		/// </summary>
		public string SerialNumber { get; set; } = "UNKNOWN";

		/// <summary>
		/// The machine's Model Number, as reported 
		/// </summary>
		public string ModelNumber { get; set; } = "UNKNOWN";

		#endregion

		#region Base class overrides

		public override string ToString()
		{
			return $"{ProductName} (SN: {SerialNumber})";
		}

		#endregion
	}
}
