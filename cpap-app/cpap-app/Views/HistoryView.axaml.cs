using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using cpap_app.Configuration;
using cpap_app.Controls;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.Printing;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace cpap_app.Views;

public partial class HistoryView : UserControl, IPrintableView
{
	#region Private fields

	private List<HistoryGraphBase> _charts      = new();
	private DispatcherTimer?       _renderTimer = null;

	#endregion

	#region Constructor

	public HistoryView()
	{
		InitializeComponent();

		AddHandler( GraphEvents.DisplayedRangeChangedEvent, OnGraphDisplayedRangeChanged );
		AddHandler( TimeSelection.TimeSelectedEvent,        OnDaySelected );
	}

	#endregion

	#region Base class overrides

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is not HistoryViewModel )
			{
				DataContext = BuildDataContext();
			}
		}
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DateRangeCombo.SelectedIndex = 0;
		CurrentDateSelection.Text    = $"{DateTime.Today:D}";
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		var ignoredSignals = new[]
		{
			SignalNames.FlowRate,
			SignalNames.EPAP,
			SignalNames.MaskPressureLow,
			SignalNames.MaskPressure,
			SignalNames.AHI,
			SignalNames.SleepStages,
			SignalNames.Movement,
		};

		const string sql = "SELECT DISTINCT signal.Name, signal.UnitOfMeasurement, signal.MinValue, signal.MaxValue FROM signal";

		using var store = StorageService.Connect();
		StorageService.CreateMapping<SignalNamesAndUnits>();

		var signalNamesAndUnits = store.Query<SignalNamesAndUnits>( sql );

		if( signalNamesAndUnits == null || signalNamesAndUnits.Count == 0 )
		{
			return;
		}

		var chartConfigs = SignalChartConfigurationStore.GetSignalConfigurations();
		foreach( var config in chartConfigs )
		{
			if( !config.IsVisible || !config.ShowInTrends || ignoredSignals.Contains( config.SignalName ) )
			{
				continue;
			}

			var signalDefaults = signalNamesAndUnits.FirstOrDefault( x => x.Name == config.SignalName );
			var units = signalDefaults?.UnitOfMeasurement;

			double? minValue = config.AxisMinValue;
			double? maxValue = config.AxisMaxValue;

			switch( config.ScalingMode )
			{
				case AxisScalingMode.Defaults:
					if (signalDefaults != null)
					{
                        minValue = signalDefaults.MinValue;
                        maxValue = signalDefaults.MaxValue;
                    }
                    break;

				case AxisScalingMode.AutoFit:
					minValue = null;
					maxValue = null;
					break;
			}

			var graph = new SignalStatisticGraph()
			{
				Title      = config.Title,
				SignalName = config.SignalName,
				MinValue   = minValue,
				MaxValue   = maxValue,
				Units      = units,
			};

			Graphs.Children.Add( graph );
		}

		foreach( var control in Graphs.Children )
		{
			if( control is HistoryGraphBase graph )
			{
				_charts.Add( graph );
			}
		}
	}

	#endregion

	#region Event handlers

	private void OnDaySelected( object? sender, DateTimeRoutedEventArgs e )
	{
		CurrentDateSelection.Text = $"{e.DateTime:D}";
	}

	private void DateRangeCombo_SelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( sender is not ComboBox combo )
		{
			return;
		}

		if( combo.SelectedItem is ComboBoxItem { Tag: string value } )
		{
			using var store = StorageService.Connect();

			var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;
			if( profileID == -1 )
			{
				return;
			}

			var lastAvailableDate = store.GetMostRecentStoredDate( profileID );
			if( lastAvailableDate <= DateHelper.UnixEpoch )
			{
				return;
			}

			// If a set number of days is defined, show only that number of days (from the last available date)
			if( int.TryParse( value, out int amount ) )
			{
				RangeStart.SelectedDate = lastAvailableDate.AddDays( -amount + 1 );
				RangeEnd.SelectedDate   = lastAvailableDate;

				DataContext = BuildDataContext();
			}
			else if( string.Equals( value, "all", StringComparison.OrdinalIgnoreCase ) )
			{
				var allDates = store.GetStoredDates( profileID );
				RangeStart.SelectedDate = allDates[ 0 ];
				RangeEnd.SelectedDate   = allDates[ ^1 ];

				DataContext = BuildDataContext();
			}
		}
	}

	private void RefreshDateRange_OnClick( object? sender, RoutedEventArgs e )
	{
		DateRangeCombo.SelectedIndex = DateRangeCombo.ItemCount - 1;
		DataContext                  = BuildDataContext();
	}

	private void OnGraphDisplayedRangeChanged( object? sender, DateTimeRangeRoutedEventArgs e )
	{
		foreach( var graph in _charts )
		{
			if( !ReferenceEquals( e.Source, graph ) )
			{
				graph.UpdateVisibleRange( e.StartTime, e.EndTime );
			}
		}

		ResetRenderTimer();
	}

	#endregion

	#region Private functions

	private void ResetRenderTimer()
	{
		_renderTimer ??= new DispatcherTimer( TimeSpan.FromSeconds( 0.25 ), DispatcherPriority.Default, ( _, _ ) =>
		{
			_renderTimer!.Stop();

			foreach( var control in _charts )
			{
				control.RenderGraph( true );
			}
		} );

		_renderTimer.Stop();
		_renderTimer.Start();
	}

	private HistoryViewModel BuildDataContext()
	{
		var start     = RangeStart.SelectedDate ?? DateTime.Today.AddDays( -90 );
		var end       = RangeEnd.SelectedDate ?? DateTime.Today;
		var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;

		return HistoryViewModel.GetHistory( profileID, start, end );
	}

	#endregion

	#region Nested types

	private class SignalNamesAndUnits
	{
		public string Name              { get; set; } = string.Empty;
		public string UnitOfMeasurement { get; set; } = string.Empty;
		public double MinValue          { get; set; }
		public double MaxValue          { get; set; }

		/// <summary>
		/// For debugging purposes.
		/// </summary>
		/// <returns>A string representation of the data.</returns>
        public override string ToString()
        {
			return $"{this.Name}, {this.MinValue} - {this.MaxValue} {this.UnitOfMeasurement}";
        }
    }

	#endregion

	#region Printing

	private void PrintReport_OnClick( object? sender, RoutedEventArgs e )
	{
		if( sender is Button button )
		{
			button.ContextFlyout!.ShowAt( button );
		}
	}

	public void PrintDocument()
	{
		PrintToPDF( this, null );
	}

	private async void PrintToPDF( object? sender, RoutedEventArgs? e )
	{
		var saveFilePath = await GetSaveFilename( "PDF" );
		if( string.IsNullOrEmpty( saveFilePath ) )
		{
			return;
		}

		var pdfDocument = new HistoryPrintDocument(
			UserProfileStore.GetActiveUserProfile(),
			_charts,
			RangeStart.SelectedDate!.Value,
			RangeEnd.SelectedDate!.Value
		);

		pdfDocument.GeneratePdf( saveFilePath );

		Process process = new Process();
		process.StartInfo = new ProcessStartInfo( saveFilePath ) { UseShellExecute = true };
		process.Start();
	}

	private async void PrintToJPG( object? sender, RoutedEventArgs e )
	{
		var saveFilePath = await GetSaveFilename( "JPG" );
		if( string.IsNullOrEmpty( saveFilePath ) )
		{
			return;
		}

		var saveFolder = Path.GetDirectoryName( saveFilePath );
		Debug.Assert( saveFolder != null, nameof( saveFolder ) + " != null" );

		var baseFilename = Path.GetFileNameWithoutExtension( saveFilePath );

		saveFilePath = Path.Combine( saveFolder, baseFilename );

		var pdfDocument = new HistoryPrintDocument(
			UserProfileStore.GetActiveUserProfile(),
			_charts,
			RangeStart.SelectedDate!.Value,
			RangeEnd.SelectedDate!.Value
		);

		var imageGenerationSettings = new ImageGenerationSettings
		{
			ImageFormat             = ImageFormat.Jpeg, 
			ImageCompressionQuality = ImageCompressionQuality.Best,
			RasterDpi               = 288 * 2,
		};

		pdfDocument.GenerateImages( index => $"{saveFilePath} Page {index + 1}.jpg", imageGenerationSettings );

		Process process = new Process();
		process.StartInfo = new ProcessStartInfo( saveFolder ) { UseShellExecute = true };
		process.Start();
	}

	private void PrintToPreviewer( object? sender, RoutedEventArgs e )
	{
		var pdfDocument = new HistoryPrintDocument(
			UserProfileStore.GetActiveUserProfile(),
			_charts,
			RangeStart.SelectedDate!.Value,
			RangeEnd.SelectedDate!.Value
		);
		
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		pdfDocument.ShowInPreviewerAsync();
#pragma warning restore CS4014
	}

	private async Task<string?> GetSaveFilename( string format )
	{
		var sp = TopLevel.GetTopLevel( this )?.StorageProvider;
		if( sp == null )
		{
			throw new Exception( $"Failed to get a reference to a {nameof( IStorageProvider )} instance." );
		}
		
		var myDocumentsFolder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
		var defaultFolder     = ApplicationSettingsStore.GetStringSetting( ApplicationSettingNames.PrintExportPath, myDocumentsFolder );
		var startFolder       = await sp.TryGetFolderFromPathAsync( defaultFolder );

		var suggestedFileName = $"Trends {RangeStart.SelectedDate:yyyy-MM-dd} to {RangeEnd.SelectedDate:yyyy-MM-dd}.{format}";

		var filePicker = await sp.SaveFilePickerAsync( new FilePickerSaveOptions()
		{
			Title                  = $"Save to {format} file",
			SuggestedStartLocation = startFolder,
			SuggestedFileName      = suggestedFileName,
			DefaultExtension       = format,
			ShowOverwritePrompt    = true,
		} );

		if( filePicker != null )
		{
			var newStartFolder = Path.GetDirectoryName( filePicker.Path.LocalPath );
			ApplicationSettingsStore.SaveStringSetting( ApplicationSettingNames.PrintExportPath, newStartFolder ?? myDocumentsFolder );
		}

		return filePicker?.Path.LocalPath;
	}

	#endregion
}
