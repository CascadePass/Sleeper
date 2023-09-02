﻿using System;
using System.Collections.Generic;

namespace cpaplib
{
	public class Signal
	{
		/// <summary>
		/// The name of the channel, such as SaO2, Flow, Mask Pressure, etc.
		/// </summary>
		public string Name { get; internal set; } = "";

		/// <summary>
		/// The interval, in seconds, between each sample. For instance if a signal is sampled at
		/// 25Hz, there will be a 40ms interval between each sample.
		/// </summary>
		public double SampleInterval { get; internal set; }

		/// <summary>
		/// The minimum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MinValue { get; internal set; }

		/// <summary>
		/// The maximum value that any sample can potentially have for this type of Signal
		/// </summary>
		public double MaxValue { get; internal set; }

		/// <summary>
		/// The signal data for this session 
		/// </summary>
		public List<double> Samples { get; } = new List<double>();
		
		/// <summary>
		/// The time when recording of this signal was started 
		/// </summary>
		public DateTime StartTime { get; internal set; }
		
		/// <summary>
		/// The time when recording of this signal was stopped 
		/// </summary>
		public DateTime EndTime { get; internal set; }

		#region Base class overrides

		public override string ToString()
		{
			return Name;
		}

		#endregion
	}
}
