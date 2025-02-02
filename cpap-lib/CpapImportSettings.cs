using System;

namespace cpaplib
{
    public class CpapImportSettings
    {
        /// <summary>
        /// Gets or sets a unique identifier for this instance
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The amount of time to add to all timestamps in imported data. Use this to compensate for clock
        /// drift or minor inaccuracies in the machine's reported time.
        /// </summary>
        public TimeSpan ClockTimeAdjustment { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets whether imported CPAP session times will be adjusted for Daylight Savings Time for
        /// the current Timezone.
        /// </summary>
        public bool UseDaylightSavingsTime { get; set; }

        /// <summary>
        /// The minimum session length (in minutes). Any imported sessions that are shorter than this
        /// value will be discarded. 
        /// </summary>
        public double MinimumSessionLength { get; set; } = 0;

        /// <summary>
        /// If set to TRUE, a <see cref="EventType.LargeLeak"/> event will be generated any time the <see cref="SignalNames.LeakRate"/>
        /// signal value exceeds the <see cref="LargeLeakThreshold"/> threshold.
        /// </summary>
        public bool FlagLargeLeaks { get; set; } = true;

        /// <summary>
        /// Sets the threshold beyond which a <see cref="EventType.LargeLeak"/> event will be generated.
        /// </summary>
        public double LargeLeakThreshold { get; set; } = 24.0;

        /// <summary>
        /// If set to TRUE, a <see cref="EventType.FlowLimitation"/> event will be generated whenever
        /// the <see cref="SignalNames.FlowLimit"/> signal value exceeds the <see cref="FlowLimitThreshold"/> threshold.
        /// </summary>
        public bool FlagFlowLimits { get; set; } = true;

        /// <summary>
        /// Sets the threshold beyond which a <see cref="EventType.FlowLimitation"/> event will be generated.
        /// </summary>
        public double FlowLimitThreshold { get; set; } = 0.3;

        /// <summary>
        /// Sets the minimum duration of a <see cref="EventType.FlowLimitation"/> event. Events shorter than this
        /// value will be discarded. 
        /// </summary>
        public double FlowLimitMinimumDuration { get; set; } = 3.0;

        /// <summary>
        /// If set to TRUE, a <see cref="EventType.FlowReduction"/> event will be generated whenever the 
        /// <see cref="SignalNames.FlowRate"/> signal is detected to have a reduced flow as defined by
        /// <see cref="FlowReductionThreshold"/>, <see cref="FlowReductionMinimumDuration"/>, and
        /// <see cref="FlowReductionWindowSize"/>
        /// </summary>
        public bool FlagFlowReductions { get; set; } = true;

        /// <summary>
        /// Sets the minimum duration of a <see cref="EventType.FlowReduction"/> event. Events shorter than this
        /// value will be discarded. 
        /// </summary>
        public double FlowReductionMinimumDuration { get; set; } = 8.0;

        /// <summary>
        /// Sets the <see cref="SignalNames.FlowRate"/> threshold below which a <see cref="EventType.FlowReduction"/> event
        /// will be generated (if the condition lasts for the duration specified by <see cref="FlowReductionMinimumDuration"/>
        /// </summary>
        public double FlowReductionThreshold { get; set; } = 0.5;

        /// <summary>
        /// When determining if the <see cref="SignalNames.FlowRate"/> signal exhibits a reduction in flow, specifies the
        /// length (in seconds) of a sliding window of the mean absolute flow rate that will be used as the baseline.  
        /// </summary>
        public double FlowReductionWindowSize { get; set; } = 120;

        /// <summary>
        /// Specifies the minimum amount of time after any "arousal breaths" (defined here as being two standard deviations
        /// larger than the sliding window mean of the absolute flow rate) that a <see cref="EventType.FlowReduction"/>
        /// event will be generated. This value is used to reduce the number of false positives generated during "sleep wake junk"
        /// arousal breathing.
        /// </summary>
        public double FlowReductionArousalDelay { get; set; } = 30.0;

        /// <summary>
        /// Returns an adjusted time, taking into account the <see cref="UseDaylightSavingsTime"/> and <see cref="ClockTimeAdjustment"/>
        /// properties to determine how whether and how much to alter the provided value.  
        /// </summary>
        public DateTime GetAdjustedTime( DateTime value )
        {
            if( UseDaylightSavingsTime && DateUtil.IsDaylightSavingTime( value ) )
            {
                value += TimeSpan.FromHours( 1 );
            }            
            
            return value + ClockTimeAdjustment;
        }
    }
}