using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;

using cpap_db.Converters;

namespace cpap_db;

public class KeyColumn
{
	public string       ColumnName { get; set; }
	public Type         Type       { get; set; }
	public string       DbType     { get; set; }
	public PropertyInfo PropertyAccessor   { get; set; }
	public bool         IsNullable { get; set; } = false;

	protected KeyColumn( string name, Type type )
	{
		ColumnName = !string.IsNullOrEmpty( name ) ? name : throw new ArgumentNullException( nameof( name ) );
		Type       = type ?? throw new ArgumentNullException( nameof( type ) );
		DbType     = DatabaseMapping.GetSqlType( type );
	}
}

public class PrimaryKeyColumn : KeyColumn
{
	public bool AutoIncrement { get; set; }

	public PrimaryKeyColumn( string name, Type type, bool autoIncrement = false ) 
		: base( name, type )
	{
		AutoIncrement = autoIncrement;
	}
}

public class ForeignKeyColumn : KeyColumn
{
	public string ReferencedTable { get; set; }
	public string ReferencedField { get; set; }

	public string OnDeleteAction { get; set; } = "CASCADE";

	public string OnUpdateAction { get; set; } = "CASCADE";

	public bool IsForeignKeyConstraintEnforced { get; set; } = true;

	public ForeignKeyColumn( string name, Type type, string referencedTable, string referencedField, bool isEnforced = true ) 
		: base( name, type )
	{
		ReferencedTable                = referencedTable;
		ReferencedField                = referencedField;
		IsForeignKeyConstraintEnforced = isEnforced;
	}

	public ForeignKeyColumn( DatabaseMapping referencedTable )
		: base( $"{referencedTable.TableName}ID", referencedTable.PrimaryKey.Type )
	{
		ReferencedTable = referencedTable.TableName;
		ReferencedField = referencedTable.PrimaryKey.ColumnName;
	}
}

public class ColumnMapping
{
	public string ColumnName      { get; set; }
	public int?   MaxStringLength { get; set; }
	public bool   IsUnique        { get; set; }
	public bool   IsNullable      { get; set; }
	
	public Type Type
	{
		get => Converter != null ? typeof( byte[] ) : _propertyType;
		set => _propertyType = value;
	}
	
	public IBlobTypeConverter Converter { get; set; }

	private PropertyInfo _property;
	private System.Type _propertyType;
	private static CultureInfo _operatingCulture;

    public ColumnMapping( string columnName, string propertyName, Type owningType )
		: this( columnName, owningType.GetProperty( propertyName, BindingFlags.Instance | BindingFlags.Public ) )
	{
		IsNullable = _property.PropertyType.IsClass;
	}

	public ColumnMapping( string name, PropertyInfo property )
	{
		ColumnName = name;
		_property  = property;
		Type       = property.PropertyType;

		Debug.Assert( _property != null && _property.CanRead && _property.CanWrite );
	}

	public object GetValue( object obj )
	{
		if( Converter != null )
		{
			return Converter.ConvertToBlob( _property.GetValue( obj ) );
		}
		
		var columnValue = _property.GetValue( obj );

		// TODO: Shouldn't be doing type conversion here, should we? Don't we already have other places that do this?
		if( columnValue is Color color )
		{
			columnValue = color.ToArgb();
		}

		return columnValue;
	}

	public void SetValue( object obj, object value )
	{
		if( Converter != null )
		{
			_property.SetValue( obj, Converter.ConvertFromBlob( value as byte[] ) );
			return;
		}

		_property.SetValue( obj, value );
	}

	public override string ToString()
	{
		return _property.ToString();
	}
	
	internal string GetDefaultValue( object defaultObject )
    {
        var defaultValue = GetValue(defaultObject);

        if (Type == typeof(string))
        {
            return $"'{defaultValue}'";
        }

        if (Type == typeof(bool))
        {
            return (bool)defaultValue ? "1" : "0";
        }

        if (Type.IsEnum)
        {
            return $"{(int)defaultValue}";
        }

        if (Type == typeof(DateTime) || Type == typeof(TimeSpan) || Type == typeof(DateTimeOffset))
        {
            return "0";
        }

		ColumnMapping.SetCulture();

        return $"{defaultValue}";
    }

    /// <summary>
    /// Sets the culture to use dot as a decimal separator, if it is not already set.
    /// </summary>
	/// <remarks>
	/// See https://github.com/CascadePass/Sleeper/issues/41 for details.
	/// </remarks>
    private static void SetCulture()
    {
        if (CultureInfo.CurrentCulture?.NumberFormat?.NumberDecimalSeparator != "." && ColumnMapping._operatingCulture == null)
        {
            var currentCulture = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
            currentCulture.NumberFormat.NumberDecimalSeparator = ".";

            ColumnMapping._operatingCulture = currentCulture;
            Thread.CurrentThread.CurrentCulture = ColumnMapping._operatingCulture;
            CultureInfo.DefaultThreadCurrentCulture = ColumnMapping._operatingCulture;
        }
    }
}

