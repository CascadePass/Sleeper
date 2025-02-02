using cpaplib;
using System;
using System.Runtime.CompilerServices;

namespace cpap_app.Helpers;

public static class DateHelper
{
	public static readonly DateTime UnixEpoch = DateTime.SpecifyKind( new DateTime( 1970, 1, 1 ), DateTimeKind.Utc ).ToLocalTime();

	public static DateTime AdjustForDaylightSavings( this DateTime dt )
	{
		if(DateUtil.IsDaylightSavingTime( dt ) )
		{
			return dt + TimeSpan.FromHours( 1 );
		}

		return dt;
	}

	public static DateTime StartOfWeek( this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Sunday )
	{
		int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
		return dt.AddDays( -1 * diff ).Date;
	}

	public static DateTime StartOfMonth( this DateTime dt )
	{
		return dt.AddDays( -1 * dt.Day + 1 ).Date;
	}

	/// <summary>
	/// Used to "fix" DateTime values retrieved through Sqlite.Net, which are incorrectly
	/// instantiated and don't contain the correct DateTimeKind value.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static DateTime AsLocalTime( this DateTime value )
	{
		return DateTime.SpecifyKind( value, DateTimeKind.Local );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static DateTime Min( DateTime a, DateTime b )
	{
		return (a < b) ? a : b;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static DateTime Max( DateTime a, DateTime b )
	{
		return (a > b) ? a : b;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static bool AreRangesDisjoint( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
	{
		return (startB > endA || endB < startA);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static bool RangesOverlap( DateTime startA, DateTime endA, DateTime startB, DateTime endB )
	{
		return !AreRangesDisjoint( startA, endA, startB, endB );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static DateTime FromMillisecondsSinceEpoch( long milliseconds )
	{
		var result = UnixEpoch.AddMilliseconds( milliseconds );
		
		var resultIsDST = DateUtil.IsDaylightSavingTime( result );
		var nowIsDst    = DateUtil.IsDaylightSavingTime( DateTime.Today );
		
		// Compensate for ToLocalTime() being incorrect for some historical dates
		return resultIsDST switch
		{
			true when !nowIsDst => result.AddHours( 1 ),
			false when nowIsDst => result.AddHours( -1 ),
			_                   => result
		};
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToMillisecondsSinceEpoch( this DateTime value )
	{
		return (long)value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static DateTime FromNanosecondsSinceEpoch( long nanoseconds )
	{
		var result = UnixEpoch.AddMilliseconds( nanoseconds * 1E-6 );

		var resultIsDST = DateUtil.IsDaylightSavingTime( result );
		var nowIsDst    = DateUtil.IsDaylightSavingTime( DateTime.Today );
		
		// Compensate for ToLocalTime() being incorrect for some historical dates
		return resultIsDST switch
		{
			true when !nowIsDst => result.AddHours( 1 ),
			false when nowIsDst => result.AddHours( -1 ),
			_                   => result
		};
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
	public static long ToNanosecondsSinceEpoch( this DateTime value )
	{
		return (long)(value.ToUniversalTime().Subtract( DateTime.UnixEpoch ).TotalMilliseconds * 1E6);
	}
}

public static class TimeSpanExtensions
{
	public static TimeSpan TrimSeconds( this TimeSpan value )
	{
		return TimeSpan.FromSeconds( Math.Truncate( value.TotalSeconds ) );
	}
	
	public static TimeSpan RoundToNearestSecond( this TimeSpan value )
	{
		return TimeSpan.FromSeconds( Math.Round( value.TotalSeconds ) );
	}
	
	public static TimeSpan RoundToNearestMinute( this TimeSpan value )
	{
		return TimeSpan.FromMinutes( Math.Round( value.TotalMinutes ) );
	}
}
