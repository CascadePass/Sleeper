using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace cpaplib
{
	public class MachineSettings : IEnumerable<KeyValuePair<string, object>>
	{
        #region Public properties

        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

		public string PressureDisplayText
		{
			get
			{
				var mode = this.OperatingMode;
                StringBuilder result = new();

				switch (mode)
				{
					case OperatingMode.Cpap:
						if (this.TryGetValue("Pressure", out double pressure))
						{
							result.Append($"{pressure}");
						}

						break;

					case OperatingMode.Apap:
                        if (this.TryGetValue("Min Pressure", out double minPressure) && this.TryGetValue("Max Pressure", out double maxPressure))
                        {
                            result.Append($"{minPressure} - {maxPressure}");

							if (this.IsEprEnabled)
							{
								result.Append($" (EPR {this.EprLevel})");
							}
                        }

                        break;

                    case OperatingMode.BilevelFixed:
						if (!this.PressureSupport.HasValue && !this.Epap.HasValue)
						{
							return "???";
						}

                        result.Append($"{Math.Round(this.PressureSupport.Value, 2)} over {Math.Round(this.Epap.Value, 2)}");

                        break;

                    case OperatingMode.BilevelAutoFixedPS:
                        if (!this.PressureSupport.HasValue && !this.Epap.HasValue)
                        {
                            return "???";
                        }

                        if (this.IpapMax.HasValue)
                        {
                            result.Append($"{Math.Round(this.PressureSupport.Value, 2)} over {Math.Round(this.Epap.Value, 2)} - {Math.Round(this.IpapMax.Value, 2)}");
                        }
                        else
                        {
                            result.Append($"{Math.Round(this.PressureSupport.Value, 2)} over {Math.Round(this.Epap.Value, 2)} - ???");
                        }

                        break;

                    case OperatingMode.Asv:
                    case OperatingMode.AsvVariableEpap:
                        //TODO: Implement ASV
                        return "NOT IMPLEMENTED YET";

                    default:
						return null;
				}

				return result.ToString();
			}
		}

		public OperatingMode OperatingMode
		{
			get
			{
                return this.GetValue<OperatingMode>(SettingNames.Mode);

    //            #region Reformat the data

    //            Dictionary<string, double> formattedData = new();

    //            if (this.Values.TryGetValue("Mode", out object value))
    //            {
    //                formattedData.Add("Mode", double.Parse(value.ToString()));
    //            }

    //            if (this.Values.TryGetValue("CPAP_MODE", out object value2))
    //            {
    //                formattedData.Add("CPAP_MODE", double.Parse(value2.ToString()));
    //            }

    //            #endregion

    //            var mode = ResMedDataLoader.GetOperatingMode(formattedData);
				//return mode;
            }
        }

        public double? Epap
		{
			get
			{
                if (this.TryGetValue(SettingNames.EPAP, out double ePap) )
                {
					return ePap;
                }

				return null;
            }
        }

        public double? Ipap
        {
            get
            {
                if (this.TryGetValue(SettingNames.IPAP, out double iPap))
                {
                    return iPap;
                }

                return null;
            }
        }

        public double? IpapMax
        {
            get
            {
                if (this.TryGetValue(SettingNames.IpapMax, out double iPapMax))
                {
                    return iPapMax;
                }

                return null;
            }
        }

        public double? PressureSupport => this.Epap.HasValue && this.Ipap.HasValue ? this.Ipap - this.Epap : null;

        public bool IsEprEnabled
        {
            get
            {
                if (this.TryGetValue("EPR Enabled", out bool eprEnabled))
                {
                    return eprEnabled;
                }

				return false;
            }
        }

        public double? EprLevel
        {
            get
            {
                if (this.TryGetValue("EPR Level", out int eprLevel))
                {
                    return eprLevel;
                }

                return null;
            }
        }

        #endregion

        #region Indexer properties

        public object this[ string key ]
		{
			get { return Values[ key ]; }
			set { Values[ key ] = value; }
		}

		#endregion

		#region Public functions

		public bool TryGetValue<T>( string key, out T value )
		{
			if( !Values.TryGetValue( key, out object storedValue ) )
			{
				value = default( T );
				return false;
			}

			value = (T)storedValue;
			return true;
		}

		public T GetValue<T>( string key )
		{
			return (T)Values[ key ];
		}

		#endregion

		#region IEnumerable interface implementation

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		#endregion
	}
}
