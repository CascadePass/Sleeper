﻿using System;
using System.Collections.Generic;
using System.Linq;

using cpap_db;

using cpaplib;

namespace cpap_app.ViewModels;

public class DailyStatisticsColumnVisibility
{
	public bool Minimum       { get; set; } = true;
	public bool Average       { get; set; } = true;
	public bool Median        { get; set; } = false;
	public bool Percentile95  { get; set; } = true;
	public bool Percentile995 { get; set; } = true;
	public bool Maximum       { get; set; } = false;
}

public class DailyStatisticsViewModel
{
	public DailyStatisticsColumnVisibility VisibleColumns { get; set; }
	public List<SignalStatistics>          Statistics     { get; set; }

	static DailyStatisticsViewModel()
	{
		using var connection = StorageService.Connect();

		var mapping = StorageService.CreateMapping<DailyStatisticsColumnVisibility>( "stats_columns" );
		mapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), false );

		connection.CreateTable<DailyStatisticsColumnVisibility>();

		if( connection.SelectById<DailyStatisticsColumnVisibility>( 0 ) == null )
		{
			var defaultValues = new DailyStatisticsColumnVisibility();
			connection.Insert( defaultValues, primaryKeyValue: 0 );
		}
	}

	public DailyStatisticsViewModel( DailyReport day )
	{
		using var db = StorageService.Connect();

		// TODO: Should column order be configurable also?
		// Retrieve the list of visible columns 
		VisibleColumns = db.SelectById<DailyStatisticsColumnVisibility>( 0 );

		// Retrieve the Signal Chart Configurations so that we can re-use the DisplayOrder values the user has configured 
		var configurations = SignalChartConfigurationStore.GetSignalConfigurations();
		
		Statistics = new List<SignalStatistics>();
		foreach( var configuration in configurations )
		{
			// Attempt to retrieve the signal statistic named by the configuration record
			var stat = day.Statistics.FirstOrDefault( x => x.SignalName.Equals( configuration.SignalName, StringComparison.OrdinalIgnoreCase ) );
			if( stat == null )
			{
				continue;
			}
			
			// If the statistic is already in our result set, move on
			if( Statistics.Any( x => x.SignalName == stat.SignalName ) )
			{
				continue;
			}
			
			// Add the statistic to the result set
			Statistics.Add( stat );

			// If the signal is configured to have a pair (such as with Pressure and EPAP), attempt to add the paired signal immediately after
			// the current signal. The paired signal (called SecondarySignal in the chart configuration) will likely be sorted near the 
			// end of the list of signals, and we want it to appear with the main signal. 
			if( !string.IsNullOrEmpty( configuration.SecondarySignalName ) )
			{
				var secondaryStat = day.Statistics.FirstOrDefault( x => x.SignalName.Equals( configuration.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) );
				if( secondaryStat == null )
				{
					continue;
				}
				
				// Make sure we haven't added it already. If so, just skip it. 
				if( Statistics.Any( x => x.SignalName.Equals( configuration.SecondarySignalName, StringComparison.OrdinalIgnoreCase ) ) )
				{
					continue;
				}

				Statistics.Add( secondaryStat );
			}
		}
		
		// Add in any statistics that have not yet been included (presumably because there is no configuration record for it)
		foreach( var stat in day.Statistics )
		{
			if( !Statistics.Any( x => x.SignalName == stat.SignalName ) )
			{
				Statistics.Add( stat );
			}
		}
		
		// Now that we have all of our Statistics in the order we want them, assign the configured names for them 
		foreach( var stat in Statistics )
		{
			var config = configurations.FirstOrDefault( x => x.SignalName.Equals( stat.SignalName, StringComparison.OrdinalIgnoreCase ) );
			if( config != null )
			{
				stat.SignalName = config.Title;
			}
		}
	}
}
