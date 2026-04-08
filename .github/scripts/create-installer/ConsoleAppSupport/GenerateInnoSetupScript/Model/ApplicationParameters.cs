using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using System.Text.RegularExpressions ;
using GenerateInnoSetupScript.Model.Attributes ;
namespace GenerateInnoSetupScript.Model
{
  public class ApplicationParameters
  {
    [ParameterName( "cfg" )]
    public string ConfigPath { get ; set ; } = "./conf/config.json" ;
    [ParameterName( "tmp" )]
    public string TemplatePath { get ; set ; } = "./conf/templateScript.iss" ;
    [ParameterName( "o" )]
    public string OutputPath { get ; set ; } = "./" ;

    public ApplicationParameters( params string[] args )
    {
      if ( args.Length % 2 > 0 ) {
        throw new ArgumentException( "Invalid command" ) ;
      }

      var properties = GetType().GetProperties() ;

      var mapProp = properties
        .Where( x => x.GetCustomAttribute<ParameterNameAttribute>() != null )
        .ToDictionary( prop => "-" + ( prop.GetCustomAttribute<ParameterNameAttribute>()?.Name ?? prop.Name.ToLower() ),
          prop => prop ) ;

      for ( int i = 0 ; i < args.Length ; i++ ) {
        if ( Regex.IsMatch( args[ i ], "-\\S+" ) ) {
          var paramName = args[ i ] ;
          var paramValue = args[ ++i ] ;
          if ( mapProp.ContainsKey( paramName ) ) {
            var prop = mapProp[ paramName ] ;

            prop.SetValue( this, paramValue ) ;
          }

        }
      }

    }
  }
}