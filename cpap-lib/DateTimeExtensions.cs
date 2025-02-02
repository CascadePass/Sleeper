using System;
using System.Runtime.CompilerServices;

namespace cpaplib
{
    internal static class DateTimeExtensions
    {
        public static DateTime Trim( this DateTime date, long ticks = TimeSpan.TicksPerSecond )
        {
            return new DateTime( date.Ticks - (date.Ticks % ticks), date.Kind );
        }
    }

    public static class DateUtil
    {
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

        public static bool IsDaylightSavingTime(DateTime dateTime)
        {
            //TODO: Use the TZ of the PAP machine

            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            return timeZoneInfo.IsDaylightSavingTime(dateTime);
        }
    }

    internal static class TimeSpanExtensions
    {
        public static DateTime AdjustImportTime( this DateTime value, CpapImportSettings settings )
        {
            return settings.GetAdjustedTime( value );
        }
        
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
}