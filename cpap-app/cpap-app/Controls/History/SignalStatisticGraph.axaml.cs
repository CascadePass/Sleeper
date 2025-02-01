using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using Avalonia;
using Avalonia.Interactivity;

using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_app.ViewModels.Tooltips;

using cpaplib;

namespace cpap_app.Controls;

public partial class SignalStatisticGraph : HistoryGraphBase
{
	#region Public properties 
	
	public static readonly StyledProperty<string>  TitleProperty      = AvaloniaProperty.Register<SignalStatisticGraph, string>( nameof( Title ) );
	public static readonly StyledProperty<string>  SignalNameProperty = AvaloniaProperty.Register<SignalStatisticGraph, string>( nameof( SignalName ) );
	public static readonly StyledProperty<string>  UnitsProperty      = AvaloniaProperty.Register<SignalStatisticGraph, string>( nameof( Units ) );
	public static readonly StyledProperty<double?> MinValueProperty   = AvaloniaProperty.Register<SignalStatisticGraph, double?>( nameof( MinValue ) );
	public static readonly StyledProperty<double?> MaxValueProperty   = AvaloniaProperty.Register<SignalStatisticGraph, double?>( nameof( MaxValue ) );

	public string Title
	{
		get => GetValue( TitleProperty );
		set => SetValue( TitleProperty, value );
	}

	public string SignalName
	{
		get => GetValue( SignalNameProperty );
		set => SetValue( SignalNameProperty, value );
	}

	public string Units
	{
		get => GetValue( UnitsProperty );
		set => SetValue( UnitsProperty, value );
	}

	public double? MinValue
	{
		get => GetValue( MinValueProperty );
		set => SetValue( MinValueProperty, value );
	}

	public double? MaxValue
	{
		get => GetValue( MaxValueProperty );
		set => SetValue( MaxValueProperty, value );
	}

	#endregion 
	
	#region Constructor 
	
	public SignalStatisticGraph()
	{
		InitializeComponent();
	}
	
	#endregion
	
	#region Base class overrides

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		GraphTitle.Text = Title;
	}

	protected override void LoadData( HistoryViewModel viewModel )
	{
		Chart.Plot.Clear();

		_history = viewModel;

		NoDataLabel.IsVisible = false;
		Chart.IsEnabled       = true;
		this.IsEnabled        = true;

		var totalDays = viewModel.TotalDays;

		var days         = viewModel.Days;
		var valuesMedian = new double[ totalDays ];
		var values99     = new double[ totalDays ];
		var values95     = new double[ totalDays ];
		var valuesMin    = new double[ totalDays ];

		foreach( var day in days )
		{
			// There may be gaps in the days, so just calculate the index for each day.
			int index = (int)Math.Floor( (day.ReportDate.Date - viewModel.Start.Date).TotalDays );
			if( index < 0 || index >= totalDays )
			{
				continue;
			}

			Debug.Assert( valuesMedian[ index ] == 0, "Duplicate index" );

			var stats = day.Statistics.FirstOrDefault( x => x.SignalName == SignalName );
			if( stats != null )
			{
				valuesMin[ index ]    = stats.Minimum;
				valuesMedian[ index ] = stats.Median;
				values95[ index ]     = stats.Percentile95;
				values99[ index ]     = stats.Percentile995;
			}
		}

		var colorIndex = ColorIndex;

		var maxChart = Chart.Plot.AddBar( values99, DataColors.GetLightThemeColor( colorIndex + 0 ).ToDrawingColor() );
		maxChart.BarWidth        = 0.95;
		maxChart.BorderLineWidth = 1;

		var percentile95Chart = Chart.Plot.AddBar( values95, DataColors.GetLightThemeColor( colorIndex + 1 ).ToDrawingColor() );
		percentile95Chart.BarWidth        = 0.95;
		percentile95Chart.BorderLineWidth = 1;

		var medianChart = Chart.Plot.AddBar( valuesMedian, DataColors.GetLightThemeColor( colorIndex + 2 ).ToDrawingColor() );
		medianChart.BarWidth        = 0.95;
		medianChart.BorderLineWidth = 1;

		var minChart = Chart.Plot.AddBar( valuesMin, DataColors.GetLightThemeColor( colorIndex + 3 ).ToDrawingColor() );
		minChart.BarWidth        = 0.95;
		minChart.BorderLineWidth = 1;

		const int DIVISIONS = 6;

		var minValue = MinValue ?? valuesMin.Min();
		var maxValue = MaxValue ?? values99.Max();
		var range    = maxValue - minValue;

		// Don't show signal stats for any Signal whose data is missing or which consists of both
		// positive and negative values (for which Statistics wouldn't be valid anyways).   
		if( range < 1e-4f || (minValue >= 0 != maxValue >= 0) )
		{
			ShowNoDataAvailable();

			return;
		}

		var positions    = new double[ DIVISIONS ];
		var labels       = new string[ DIVISIONS ];
		var wholeNumbers = true;

		for( int i = 0; i < DIVISIONS; i++ )
		{
			positions[ i ] =  minValue + i * (range / (DIVISIONS - 1));
			wholeNumbers   &= positions[ i ] % 1.0 == 0.0;
		}

		for( int i = 0; i < DIVISIONS; i++ )
		{
			labels[ i ] = wholeNumbers ? $"{positions[ i ]}" : $"{positions[ i ]:F2}";
		}

		Chart.Plot.YAxis.ManualTickPositions( positions, labels );
		Chart.Plot.SetAxisLimitsY( minValue, maxValue );
		Chart.Plot.YAxis.SetBoundary( minValue, maxValue );
		
		// NOTE: For some reason, we need to offset the beginning and end to account for centered bar offsets
		Chart.Plot.SetAxisLimitsX( -0.5, totalDays - 0.5 );
		Chart.Plot.XAxis.SetBoundary( -0.5, totalDays - 0.5 );
		
		_selectionSpan                = Chart.Plot.AddHorizontalSpan( -1, -1, Color.Red.MultiplyAlpha( 0.35f ), null );
		_selectionSpan.IgnoreAxisAuto = true;
		_selectionSpan.IsVisible      = false;

		RenderGraph( true );
	}

	protected override object? BuildTooltipDataContext( DailyReport day )
	{
		var stats = day.Statistics.FirstOrDefault( x => x.SignalName == SignalName );

		return new SignalStatisticsViewModel()
		{
			Date       = day.ReportDate.Date,
			Statistics = stats,
			Units      = Units,
		};
	}

    public override string ToString()
    {
		return Title;
    }

    #endregion
}

