using System.Collections.Generic ;
using System.IO ;
using System.Text.Json ;
namespace GenerateInnoSetupScript.Model.GenCode
{
  public class Config
  {
    public List<Version> Versions { get ; set ; }
    public AppInfo AppInfo { get ; set ; }
    public List<Module> Modules { get ; set ; }

    public static Config LoadConfigFromFile(string configJsonFile)
    {
      var confFileInfo = new FileInfo( configJsonFile ) ;
      if ( ! confFileInfo.Exists ) {
        throw new DirectoryNotFoundException( $"Config file \"{configJsonFile}\" does not existed." ) ;
      }
      string json = "" ;
      using ( StreamReader r = new StreamReader( confFileInfo.OpenRead() ) ) {
        json = r.ReadToEnd() ;
      }
      return JsonSerializer.Deserialize<Config>( json ) ;
    }
  }
}