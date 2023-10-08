﻿using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;

using cpaplib;

using SQLite;

using SQLitePCL;

namespace cpap_db
{
	public static class TableNames
	{
		public const string DailyReport         = "day";
		public const string Session             = "session";
		public const string Signal              = "signal";
		public const string FaultMapping        = "fault";
		public const string ReportedEvent       = "event";
		public const string SignalStatistics    = "signal_stats";
		
		public const string MachineSettings = "machine_settings";
		public const string MachineInfo     = "machine_info";
		
		// All of the following classes will likely be eliminated at some point, but for now...
		public const string EprSettings         = "epr_settings";
		public const string CpapSettings        = "cpap_settings";
		public const string AutoSetSettings     = "auto_settings";
		public const string AsvSettings         = "asv_settings";
		public const string AvapSettings       = "avaps_settings";
	}
	
	public class StorageService : IDisposable
	{
		#region Private fields

		public SQLiteConnection Connection { get; private set; } = null;

		private static Dictionary<System.Type, DatabaseMapping> _mappings = new();
		
		#endregion 
		
		#region Class Constructor

		static StorageService()
		{
			#region Create mappings for cpap-lib types

			var dayMapping = CreateMapping<DailyReport>( TableNames.DailyReport );
			dayMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( DateTime ) );
			dayMapping.ForeignKey = new ForeignKeyColumn( "profileID", typeof( int ), "profile", "profileID", false );

			var machineInfoMapping = CreateMapping<MachineIdentification>( TableNames.MachineInfo );
			machineInfoMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var faultInfoMapping = CreateMapping<FaultInfo>( TableNames.FaultMapping );
			faultInfoMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
			faultInfoMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var eventMapping = CreateMapping<ReportedEvent>( TableNames.ReportedEvent );
			eventMapping.ForeignKey = new ForeignKeyColumn( dayMapping );
			
			var sessionMapping = CreateMapping<Session>( TableNames.Session );
			sessionMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
			sessionMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var blobColumnMapping = new ColumnMapping( "samples", "Samples", typeof( Signal ) );
			blobColumnMapping.Converter = new DoubleListBlobConverter();

			var signalMapping = CreateMapping<Signal>( TableNames.Signal );
			signalMapping.ForeignKey = new ForeignKeyColumn( sessionMapping );
			signalMapping.Columns.Add( blobColumnMapping );

			var signalStatisticsMapping = CreateMapping<SignalStatistics>( TableNames.SignalStatistics );
			signalStatisticsMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var eventsMapping = CreateMapping<ReportedEvent>( TableNames.ReportedEvent );
			eventsMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var machineSettingsMapping = CreateMapping<MachineSettings>( TableNames.MachineSettings );
			machineSettingsMapping.PrimaryKey = new PrimaryKeyColumn( "id", typeof( int ), true );
			machineSettingsMapping.ForeignKey = new ForeignKeyColumn( dayMapping );

			var eprMapping = CreateMapping<EprSettings>( TableNames.EprSettings );
			eprMapping.ForeignKey = new ForeignKeyColumn( machineSettingsMapping );

			var cpapMapping = CreateMapping<CpapSettings>( TableNames.CpapSettings );
			cpapMapping.ForeignKey = new ForeignKeyColumn( machineSettingsMapping );

			var autosetMapping = CreateMapping<AutoSetSettings>( TableNames.AutoSetSettings );
			autosetMapping.ForeignKey = new ForeignKeyColumn( machineSettingsMapping );

			var asvMapping = CreateMapping<AsvSettings>( TableNames.AsvSettings );
			asvMapping.ForeignKey = new ForeignKeyColumn( machineSettingsMapping );

			var avapMapping = CreateMapping<AvapSettings>( TableNames.AvapSettings );
			avapMapping.ForeignKey = new ForeignKeyColumn( machineSettingsMapping );

			#endregion
		}
		
		#endregion
		
		#region Instance Constructor 

		internal StorageService( string databasePath )
		{
			Connection = new SQLiteConnection( databasePath );
		}

		#endregion
		
		#region Static functions

		public static string GetApplicationDatabasePath()
		{
			var appDataPath    = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
			var databaseFolder = Path.Combine( appDataPath,    "cpap-viewer" );
			var databasePath   = Path.Combine( databaseFolder, "cpap-data.db" );

			if( !Directory.Exists( databaseFolder ) )
			{
				Directory.CreateDirectory( databaseFolder );
			}

			return databasePath;
		}
		
		public static void InitializeDatabase( string databasePath )
		{
			using( var store = new StorageService( databasePath ) )
			{
				foreach( var mapping in _mappings )
				{
					Debug.WriteLine( $"Checking database table {mapping.Key.Name}" );
					mapping.Value.CreateTable( store.Connection );
				}
			}
		}

		public static StorageService Connect()
		{
			return new StorageService( GetApplicationDatabasePath() );
		}
		
		public static DatabaseMapping<T> GetMapping<T>() where T: class, new()
		{
			if( _mappings.TryGetValue( typeof( T ), out DatabaseMapping mapping ) )
			{
				return (DatabaseMapping<T>)mapping;
			}

			return null;
		}
		
		public static DatabaseMapping<T> CreateMapping<T>( string tableName = null ) where T : class, new()
		{
			var mapping = new DatabaseMapping<T>( tableName ?? typeof( T ).Name );
			_mappings[ typeof( T ) ] = mapping;

			return mapping;
		}

		#endregion 
		
		#region Public functions
		
		public DailyReport LoadDailyReport( DateTime date )
		{
			var day = SelectById<DailyReport>( date );
			if( day == null )
			{
				return null;
			}
			
			day.MachineInfo      = SelectByForeignKey<MachineIdentification>( date ).First();
			day.Fault            = SelectByForeignKey<FaultInfo>( date ).First();
			day.Statistics       = SelectByForeignKey<SignalStatistics>( date );
			day.Events           = SelectByForeignKey<ReportedEvent>( date );
			day.Settings         = SelectByForeignKey<MachineSettings>( date, out int settingsID );
			day.Settings.AutoSet = SelectByForeignKey<AutoSetSettings>( settingsID ).First();
			day.Settings.CPAP    = SelectByForeignKey<CpapSettings>( settingsID ).First();
			day.Settings.Avap    = SelectByForeignKey<AvapSettings>( settingsID ).First();
			day.Settings.ASV     = SelectByForeignKey<AsvSettings>( settingsID ).First();
			day.Settings.EPR     = SelectByForeignKey<EprSettings>( settingsID ).First();

			var sessionKeys = new List<int>();
			day.Sessions = SelectByForeignKey<Session, int>( date, sessionKeys );

			Debug.Assert( sessionKeys.Count == day.Sessions.Count );
			for( int i = 0; i < sessionKeys.Count; i++ )
			{
				day.Sessions[ i ].Signals = SelectByForeignKey<Signal>( sessionKeys[ i ] );
			}
			
			day.Statistics.Sort();
			day.Events.Sort();
			day.Sessions.Sort();

			return day;
		}

		public void SaveDailyReport( DailyReport day )
		{
			var dayID = day.ReportDate.Date;

			// Delete any existing record first. This will not cause any exception if the record does
			// not already exist, and there's no convenient way to update existing records with this 
			// many nested dependencies, so just get rid of it if it already exists. 
			var mapping = GetMapping<DailyReport>();
			mapping.Delete( Connection, dayID );

			Insert( day, dayID, -1 );
			
			Insert( day.MachineInfo, foreignKeyValue: dayID );
			Insert( day.Fault,       foreignKeyValue: dayID );

			int settingsID = Insert( day.Settings, foreignKeyValue: dayID );
			Insert( day.Settings.AutoSet, foreignKeyValue: settingsID );
			Insert( day.Settings.ASV, foreignKeyValue: settingsID );
			Insert( day.Settings.CPAP, foreignKeyValue: settingsID );
			Insert( day.Settings.EPR, foreignKeyValue: settingsID );
			Insert( day.Settings.Avap, foreignKeyValue: settingsID );

			foreach( var evt in day.Events )
			{
				Insert( evt, foreignKeyValue: dayID );
			}

			foreach( var stat in day.Statistics )
			{
				Insert( stat, foreignKeyValue: dayID );
			}
			
			foreach( var session in day.Sessions )
			{
				var sessionID = Insert( session, foreignKeyValue: dayID );

				foreach( var signal in session.Signals )
				{
					Insert( signal, foreignKeyValue: sessionID );
				}
			}
		}

		public List<T> SelectAll<T>() where T : class, new()
		{
			var mapping = GetMapping<T>();
			return mapping.ExecuteQuery( Connection, mapping.SelectAllQuery );
		}

		public List<T> SelectAll<T, P>( IList<P> primaryKeys ) 
			where T : class, new()
			where P : struct
		{
			var mapping = GetMapping<T>();
			
			return mapping.ExecuteQuery( Connection, mapping.SelectAllQuery, primaryKeys );
		}

		public T SelectById<T>( object primaryKeyValue ) where T : class, new()
		{
			var mapping = GetMapping<T>();
			return mapping.SelectByPrimaryKey( Connection, primaryKeyValue );
		}

		public T SelectByForeignKey<T>( object foreignKeyValue, out int primaryKeyValue ) where T : class, new()
		{
			var mapping = GetMapping<T>();

			var keys    = new List<int>();
			var records = mapping.SelectByForeignKey<int>( Connection, foreignKeyValue, keys );

			primaryKeyValue = keys.First();

			return records.First();
		}
		
		public List<T> SelectByForeignKey<T>( object foreignKeyValue ) where T : class, new()
		{
			var mapping = GetMapping<T>();
			return mapping.SelectByForeignKey( Connection, foreignKeyValue );
		}
		
		public List<T> SelectByForeignKey<T, P>( object foreignKeyValue, IList<P> primaryKeys ) 
			where T : class, new()
			where P : struct
		{
			var mapping = GetMapping<T>();
			return mapping.SelectByForeignKey<P>( Connection, foreignKeyValue, primaryKeys );
		}
		
		/// <summary>
		/// Inserts a new record into the mapped table. 
		/// </summary>
		/// <param name="record">The object to be inserted</param>
		/// <param name="primaryKeyValue">If a primary key is defined in the table mapping and is not auto-increment, it must be supplied here</param>
		/// <param name="foreignKeyValue">If a foreign key is defined in the table mapping, it must be supplied here</param>
		/// <typeparam name="T">If the primary key is defined as AUTOINCREMENT, it will be returned. Otherwise, the number of records inserted (1) will be returned.</typeparam>
		/// <returns></returns>
		public int Insert<T>( T record, object primaryKeyValue = null, object foreignKeyValue = null ) where T : class, new()
		{
			var mapping = GetMapping<T>();
			var result = mapping.Insert( Connection, record, primaryKeyValue, foreignKeyValue );

			if( mapping.PrimaryKey is { AutoIncrement: true } && mapping.PrimaryKey.PropertyAccessor != null )
			{
				mapping.PrimaryKey.PropertyAccessor.SetValue( record, result );
			}

			return result;
		}

		public bool CreateTable<T>() where T : class, new()
		{
			var mapping = GetMapping<T>();
			if( mapping == null )
			{
				throw new NotSupportedException( $"No mapping has been defined for {typeof( T ).Name}" );
			}

			return mapping.CreateTable( Connection );
		}

		public List<DateTime> GetStoredDates()
		{
			var mapping = GetMapping<DailyReport>();
			var days    = mapping.SelectAll( Connection );

			return days.OrderBy( x => x.ReportDate ).Select( x => x.ReportDate.Date ).ToList();
		}
		
		public DateTime GetMostRecentStoredDate()
		{
			var mapping = GetMapping<DailyReport>();
			var query   = $"SELECT IFNULL( [{mapping.PrimaryKey.ColumnName}], datetime('now','-180 day','localtime') ) FROM [{mapping.TableName}] ORDER BY ReportDate DESC LIMIT 1";
			
			return Connection.ExecuteScalar<DateTime>( query );
		}

		public int Execute( string query, params object[] arguments )
		{
			return Connection.Execute( query, arguments );
		}

		public List<DataRow> Query( string query, params object[] arguments )
		{
			List<DataRow> result = new();

			var stmt = SQLite3.Prepare2( Connection.Handle, query );

			int argumentIndex = 1;
			foreach( var arg in arguments )
			{
				BindParameter( stmt, argumentIndex++, arg );
			}

			try
			{
				while( SQLite3.Step( stmt ) == SQLite3.Result.Row )
				{
					var row = new DataRow();

					int columnCount = SQLite3.ColumnCount( stmt );
					for( int index = 0; index < columnCount; ++index )
					{
						var columnName  = SQLite3.ColumnName( stmt, index );
						var columnType  = SQLite3.ColumnType( stmt, index );
						var columnValue = ReadColumn( Connection, stmt, index, columnType );

						row[ columnName ] = columnValue;
					}

					result.Add( row );
				}
			}
			finally
			{
				SQLite3.Finalize( stmt );
			}

			return result;
		}

		#endregion 
		
		#region Private functions

		internal static void BindParameter( sqlite3_stmt stmt, int index, object value )
		{
			switch( value )
			{
				case null:
					SQLite3.BindNull( stmt, index );
					break;
				case int val:
					SQLite3.BindInt( stmt, index, val );
					break;
				case string s:
					SQLite3.BindText( stmt, index, s, -1, new IntPtr( -1 ) );
					break;
				case byte _:
				case ushort _:
				case sbyte _:
				case short _:
					SQLite3.BindInt( stmt, index, Convert.ToInt32( value ) );
					break;
				case bool flag:
					SQLite3.BindInt( stmt, index, flag ? 1 : 0 );
					break;
				case uint _:
				case long _:
					SQLite3.BindInt64( stmt, index, Convert.ToInt64( value ) );
					break;
				case float _:
				case double _:
				case decimal _:
					SQLite3.BindDouble( stmt, index, Convert.ToDouble( value ) );
					break;
				case TimeSpan span:
					SQLite3.BindInt64( stmt, index, span.Ticks );
					break;
				case DateTime time:
					SQLite3.BindInt64( stmt, index, time.Ticks );
					break;
				case DateTimeOffset dateTimeOffset:
					SQLite3.BindInt64( stmt, index, dateTimeOffset.UtcTicks );
					break;
				case byte[] bytes:
					SQLite3.BindBlob( stmt, index, bytes, bytes.Length, new IntPtr( -1 ) );
					break;
				case Guid guid:
					SQLite3.BindText( stmt, index, guid.ToString(), 72, new IntPtr( -1 ) );
					break;
				case Uri uri:
					SQLite3.BindText( stmt, index, uri.ToString(), -1, new IntPtr( -1 ) );
					break;
				case StringBuilder builder:
					SQLite3.BindText( stmt, index, builder.ToString(), -1, new IntPtr( -1 ) );
					break;
				case UriBuilder builder:
					SQLite3.BindText( stmt, index, builder.ToString(), -1, new IntPtr( -1 ) );
					break;
				case Color color:
					SQLite3.BindInt( stmt, index, color.ToArgb() );
					break;
				default:
					int int32 = Convert.ToInt32( value );
					SQLite3.BindInt( stmt, index, int32 );
					break;
			}
		}

		internal static object ReadColumn( SQLiteConnection connection, sqlite3_stmt stmt, int index, SQLite3.ColType type )
		{
			return type switch
			{
				SQLite3.ColType.Integer => SQLite3.ColumnInt( stmt, index ),
				SQLite3.ColType.Float   => SQLite3.ColumnDouble( stmt, index ),
				SQLite3.ColType.Text    => SQLite3.ColumnString( stmt, index ),
				SQLite3.ColType.Blob    => SQLite3.ColumnByteArray( stmt, index ),
				SQLite3.ColType.Null    => null,
				_                       => throw new ArgumentOutOfRangeException( nameof( type ), type, null )
			};
		}
		
		internal static object ReadColumn( SQLiteConnection connection, sqlite3_stmt stmt, int index, SQLite3.ColType type, Type clrType )
		{
			if( type == SQLite3.ColType.Null )
			{
				return null;
			}

			TypeInfo typeInfo = clrType.GetTypeInfo();

			if( typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof( Nullable<> ) )
			{
				clrType  = typeInfo.GenericTypeArguments[ 0 ];
				typeInfo = clrType.GetTypeInfo();
			}

			if( clrType == typeof( string ) )
			{
				return SQLite3.ColumnString( stmt, index );
			}

			if( clrType == typeof( int ) )
			{
				return SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( bool ) )
			{
				return (SQLite3.ColumnInt( stmt, index ) == 1);
			}

			if( clrType == typeof( double ) )
			{
				return SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( float ) )
			{
				return (float)SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( TimeSpan ) )
			{
				if( connection.StoreTimeSpanAsTicks )
				{
					return new TimeSpan( SQLite3.ColumnInt64( stmt, index ) );
				}
			}

			if( clrType == typeof( DateTime ) )
			{
				return new DateTime( SQLite3.ColumnInt64( stmt, index ) );
			}

			if( clrType == typeof( DateTimeOffset ) )
			{
				return new DateTimeOffset( SQLite3.ColumnInt64( stmt, index ), TimeSpan.Zero );
			}

			if( typeInfo.IsEnum )
			{
				if( type != SQLite3.ColType.Text )
				{
					return SQLite3.ColumnInt( stmt, index );
				}

				string str = SQLite3.ColumnString( stmt, index );
				return Enum.Parse( clrType, str.ToString(), true );
			}

			if( clrType == typeof( long ) )
			{
				return SQLite3.ColumnInt64( stmt, index );
			}

			if( clrType == typeof( uint ) )
			{
				return (uint)SQLite3.ColumnInt64( stmt, index );
			}

			if( clrType == typeof( Decimal ) )
			{
				return (Decimal)SQLite3.ColumnDouble( stmt, index );
			}

			if( clrType == typeof( byte ) )
			{
				return (byte)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( ushort ) )
			{
				return (ushort)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( short ) )
			{
				return (short)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( sbyte ) )
			{
				return (sbyte)SQLite3.ColumnInt( stmt, index );
			}

			if( clrType == typeof( byte[] ) )
			{
				return SQLite3.ColumnByteArray( stmt, index );
			}

			if( clrType == typeof( Guid ) )
			{
				return new Guid( SQLite3.ColumnString( stmt, index ) );
			}

			if( clrType == typeof( Uri ) )
			{
				return new Uri( SQLite3.ColumnString( stmt, index ) );
			}

			if( clrType == typeof( Color ) )
			{
				return Color.FromArgb( SQLite3.ColumnInt( stmt, index ) );
			}

			throw new NotSupportedException( $"Unhandled type {clrType}" );
		}

		#endregion 
		
		#region IDisposable interface implementation
		
		public void Dispose()
		{
			Connection.Dispose();
			Connection = null;
		}
		
		#endregion 
		
		#region Nested types

		public class DataRow
		{
			#region Public properties
			
			public int ColumnCount { get => _columns.Count; }
			
			#endregion 
			
			#region Private fields

			private List<KeyValuePair<string, object>> _columns = new();

			#endregion
			
			#region Public functions

			public string GetColumnName( int index )
			{
				return _columns[ index ].Key;
			}

			public object GetValue( int index )
			{
				return _columns[ index ].Value;
			}
			
			#endregion 
			
			#region Indexer functions

			public object this[ int index ]
			{
				get => _columns[ index ].Value;
				set => _columns[ index ] = new KeyValuePair<string, object>( _columns[ index ].Key, value );
			}

			public object this[ string key ]
			{
				get
				{
					foreach( var column in _columns )
					{
						if( column.Key.Equals( key, StringComparison.Ordinal ) )
						{
							return column.Value;
						}
					}

					throw new KeyNotFoundException( $"There is no column named {key}" );
				}
				set
				{
					for( int i = 0; i < _columns.Count; i++ )
					{
						if( _columns[ i ].Key.Equals( key, StringComparison.Ordinal ) )
						{
							_columns[ i ] = new KeyValuePair<string, object>( _columns[ i ].Key, value );
							return;
						}
					}

					_columns.Add( new KeyValuePair<string, object>( key, value ) );
				}
			}
			
			#endregion 
		}
		
		#endregion
	}
}
