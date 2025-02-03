using System;
using System.Collections.Generic;

namespace cpaplib
{
    /// <summary>
    /// A signal processing filter being used for breath detection.
    /// </summary>
    /// <seealso cref="https://en.wikipedia.org/wiki/Butterworth_filter"/>
    internal class ButterworthFilter
	{
		public static void FilterInPlace( List<double> sourceData, double sourceFrequency, double cutOff )
		{
			var result = Filter( sourceData.ToArray(), sourceFrequency, cutOff );
			sourceData.Clear();
			sourceData.AddRange( result );
		}
		
		public static double[] Filter( double[] sourceData, double sourceFrequency, double cutOff )
		{
			if( cutOff == 0 ) return sourceData;

			long     sourceLength = sourceData.Length;
			double[] workBuffer   = new double[ sourceLength + 4 ];

			// Copy source data to a buffer which has two extra elements at the beginning and end 
			Array.Copy( sourceData, 0, workBuffer, 2, sourceLength );
			workBuffer[ 1 ]                = workBuffer[ 0 ]                = sourceData[ 0 ];
			workBuffer[ sourceLength + 3 ] = workBuffer[ sourceLength + 2 ] = sourceData[ sourceLength - 1 ];

			const double pi = 3.14159265358979;

			double wc = Math.Tan( cutOff * pi / sourceFrequency );
			double k1 = 1.414213562 * wc; // Sqrt(2) * wc
			double k2 = wc * wc;
			double a  = k2 / (1 + k1 + k2);
			double b  = 2 * a;
			double k3 = b / k2;
			double d  = -2 * a + k3;
			double e  = 1 - (2 * a) - k3;

			double[] dataY          = new double[ sourceLength + 4 ];
			dataY[ 1 ] = dataY[ 0 ] = sourceData[ 0 ];

			for( long s = 2; s < sourceLength + 2; s++ )
			{
				dataY[ s ] = a * workBuffer[ s ] +
				             b * workBuffer[ s - 1 ] +
				             a * workBuffer[ s - 2 ] +
				             d * dataY[ s - 1 ] +
				             e * dataY[ s - 2 ];
			}

			dataY[ sourceLength + 3 ] = dataY[ sourceLength + 2 ] = dataY[ sourceLength + 1 ];

			double[] dataZ = new double[ sourceLength + 2 ];
			dataZ[ sourceLength ]     = dataY[ sourceLength + 2 ];
			dataZ[ sourceLength + 1 ] = dataY[ sourceLength + 3 ];

			for( long t = sourceLength - 1; t >= 0; t-- )
			{
				dataZ[ t ] = a * dataY[ t + 2 ] +
				             b * dataY[ t + 3 ] +
				             a * dataY[ t + 4 ] +
				             d * dataZ[ t + 1 ] +
				             e * dataZ[ t + 2 ];
			}

			double[] result = new double[ sourceLength ];
			Array.Copy( dataZ, 0, result, 0, sourceLength );

			return result;
		}
	}
}
