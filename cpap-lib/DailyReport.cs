﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace cpaplib
{
	public class DailyReport : IComparable<DailyReport>
	{
		#region Public properties

		/// <summary>
		/// The unique identifier for this instance
		/// </summary>
		public int ID { get; set; } = -1;
		
		/// <summary>
		/// The date for which this report was generated (ie, "Report for the night of Dec 12, 2023")
		/// </summary>
		public DateTime ReportDate { get; set; }
		
		/// <summary>
		/// The specific time at which recording began
		/// </summary>
		public DateTime RecordingStartTime { get; set; }
		
		/// <summary>
		/// The specific time at which recording ended
		/// </summary>
		public DateTime RecordingEndTime { get; set; }
		
		/// <summary>
		/// Returns the total time between the start of the first session and the end of the last session
		/// </summary>
		public TimeSpan TotalTimeSpan { get => RecordingEndTime - RecordingStartTime; }

		/// <summary>
		/// Returns true if this instance contains detailed session data, or false if not (such as when the user
		/// did not have an SD card in the machine) 
		/// </summary>
		public bool HasSessionData
		{
			get
			{
				return Sessions.Count > 0 && Sessions.Any( x => x.Signals.Count > 0 );
			}
		}

		/// <summary>
		/// Returns the number of "Mask Times" for the day
		/// </summary>
		internal int MaskEvents { get; set; }

		/// <summary>
		/// The total amount of time the CPAP was used on the reported day (calculated)
		/// </summary>
		public TimeSpan TotalSleepTime { get; set; }

		/// <summary>
		/// Returns the total number of hours the patient has used the CPAP machine since the last factory reset.
		/// Supported on ResMed AirSense machines, not sure about others. 
		/// </summary>
		public double PatientHours { get; set; }

		/// <summary>
		/// Identifies the machine that was used to record this report 
		/// </summary>
		public MachineIdentification MachineInfo { get; set; } = new MachineIdentification();
		
		/// <summary>
		/// Fault information reported by the CPAP machine
		/// </summary>
		public FaultInfo Fault { get; set; } = new FaultInfo();

		/// <summary>
		/// The settings (pressure, EPR, response type, etc.) used on this day
		/// </summary>
		public MachineSettings Settings { get; set; } = new MachineSettings();

		/// <summary>
		/// The list of sessions  for this day
		/// </summary>
		public List<Session> Sessions { get; set; } = new List<Session>();

		public List<ReportedEvent> Events { get; set; } = new List<ReportedEvent>();

		/// <summary>
		/// Usage and performance statistics for this day (average pressure, leak rate, etc.)
		/// </summary>
		public List<SignalStatistics> Statistics { get; set; } = new List<SignalStatistics>();

		/// <summary>
		/// Any notes that should be saved along with this daily report
		/// </summary>
		public string Notes { get; set; } = string.Empty;

		/// <summary>
		/// The list of <see cref="Annotation"/> items entered by the user for this <see cref="DailyReport"/>
		/// </summary>
		public List<Annotation> Annotations { get; set; } = new List<Annotation>();

		#endregion 

		#region Public functions

		/// <summary>
		/// Recalculates the statistics for the named Signal. Designed to be called after a data import to
		/// update the statistics to account for the newly imported data. 
		/// </summary>
		public void UpdateSignalStatistics( string signalName )
		{
			Statistics.RemoveAll( x => x.SignalName.Equals( signalName ) );

			var calculator = new SignalStatCalculator();
			var stats      = calculator.CalculateStats( signalName, Sessions );

			if( stats != null )
			{
				Statistics.Add( stats );
			}
		}
		
		/// <summary>
		/// Updates the RecordingStartTime, RecordingEndTime, and TotalSleepTime properties to reflect
		/// any changes to the Sessions collection.
		/// </summary>
		public void RefreshTimeRange()
		{
			if( HasSessionData )
			{
				RecordingStartTime = Sessions.Min( x => x.StartTime );
				RecordingEndTime   = Sessions.Max( x => x.EndTime );
				TotalSleepTime     = CalculateTotalSleepTime();
			}
			// else
			// {
			// 	RecordingStartTime = ReportDate.Date.AddHours( 12 );
			// 	RecordingEndTime   = RecordingStartTime;
			// 	TotalSleepTime     = TimeSpan.Zero;
			// }
		}

		/// <summary>
		/// Adds a new Session to the Sessions list and updates the RecordingStartTime and RecordingEndTime
		/// properties if necessary.
		/// </summary>
		/// <param name="session"></param>
		public void AddSession( Session session )
		{
			Sessions.Add( session );
			
			RecordingStartTime = DateUtil.Min( RecordingStartTime, session.StartTime );
			RecordingEndTime   = DateUtil.Max( RecordingEndTime, session.EndTime );

			TotalSleepTime = CalculateTotalSleepTime();

			Sessions.Sort();
		}

		/// <summary>
		/// Removes a Session from the Sessions list, as well as any associated events. Updates the
		/// signal statistics for all Signals in the Session, and updates the RecordingStartTime and
		/// RecordingEndTime properties if necessary. 
		/// </summary>
		protected bool RemoveSession( Session session )
		{
			if( !Sessions.Remove( session ) )
			{
				return false;
			}

			Events.RemoveAll( x =>
				x.SourceType == session.SourceType &&
				x.StartTime >= session.StartTime &&
				x.StartTime <= session.EndTime
			);

			foreach( var signal in session.Signals )
			{
				if( Statistics.Any( x => x.SignalName == signal.Name ) )
				{
					UpdateSignalStatistics( signal.Name );
				}
			}

			RefreshTimeRange();

			return true;
		}
		
		/// <summary>
		/// Calculates the TotalSleepTime value by adding the duration of all sessions generated by a CPAP machine.
		/// </summary>
		/// <returns></returns>
		private TimeSpan CalculateTotalSleepTime()
		{
			// Total Sleep Time only refers to sessions that were generated by the CPAP
			return TimeSpan.FromSeconds( Sessions.Where( x => x.SourceType == SourceType.CPAP ).Sum( x => x.Duration.TotalSeconds ) );
		}

		/// <summary>
		/// Merges Session data with existing Sessions when possible, or adds it if there are no coincident Sessions
		/// to merge with. Note that the Session being passed must still overlap the time period of this DailyReport,
		/// and an exception will be thrown if that is not the case.  
		/// </summary>
		public void MergeSession( Session session )
		{
			if( RecordingStartTime > session.EndTime || RecordingEndTime < session.StartTime )
			{
				throw new Exception( $"Session from {session.StartTime} to {session.EndTime} does not overlap reporting period for {ReportDate.Date} and cannot be merged." );
			}
			
			// There are two obvious options here: Merge the new session's signals into an existing session, or 
			// simply add the new session to the list of the day's sessions. 
			//
			// The first option will potentially involve fixing up start and end times, and might have more 
			// non-obvious edge cases to worry about, but the second option creates a situation where all 
			// sessions only contain a subset of the available data for a given period of time, which sort of
			// breaks the original design of what a session entails.
			//
			// When merging into an existing session, there is the question of whether to extend the session
			// start and end times if the new data exceeds them, or whether to trim the new data to match 
			// those times if needed. Consider adding pulse oximetry data to a session containing CPAP flow
			// pressure data (among others) when the pulse oximetry data starts a few seconds before the CPAP
			// data starts and ends a minute after the CPAP session ends; The user probably cares more about
			// the CPAP data and wants to see what their blood oxygen levels were during the CPAP therapy,
			// and may not necessarily care about values that lie outside of the "mask on" period. 
			//

			foreach( var existingSession in Sessions )
			{
				bool disjoint = (existingSession.StartTime > session.EndTime || existingSession.EndTime < session.StartTime);
				if( !disjoint )
				{
					// When merging with an existing Session, all of the merged Signals will be trimmed to fit the 
					// destination Session's time period. 
					existingSession.Merge( session );
					
					return;
				}
			}

			Sessions.Add( session );
			Sessions.Sort( ( lhs, rhs ) => lhs.StartTime.CompareTo( rhs.StartTime ) );
			
			RecordingStartTime = DateUtil.Min( RecordingStartTime, session.StartTime );
			RecordingEndTime   = DateUtil.Max( RecordingEndTime, session.EndTime );

			TotalSleepTime = CalculateTotalSleepTime();
		}

		public static bool TimesOverlap( DailyReport day, Session session )
		{
			return DateHelper.RangesOverlap( day.RecordingStartTime, day.RecordingEndTime, session.StartTime, session.EndTime );
		}

		#endregion
		
		#region IComparable<DailyReport> interface implementation 
		
		public int CompareTo( DailyReport other )
		{
			return other == null ? 0 : ReportDate.Date.CompareTo( other.ReportDate.Date );
		}

		#endregion 

		#region Base class overrides

		public override string ToString()
		{
			if( Sessions.Count > 0 )
			{
				return $"{ReportDate.ToLongDateString()}   {Sessions.First().StartTime.ToShortTimeString()} - {Sessions.Last().EndTime.ToShortTimeString()}    ({TotalSleepTime})";
			}

			return $"{ReportDate.ToLongDateString()}   ({TotalSleepTime})";
		}

		#endregion
	}
}
