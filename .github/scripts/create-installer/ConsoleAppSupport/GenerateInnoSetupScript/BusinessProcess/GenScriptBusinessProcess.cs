using System.IO ;
using System.Linq ;
using System.Text ;
using Fluid ;
using GenerateInnoSetupScript.Exceptions ;
using GenerateInnoSetupScript.Extensions ;
using GenerateInnoSetupScript.Model ;
using GenerateInnoSetupScript.Model.GenCode ;
using Version = GenerateInnoSetupScript.Model.GenCode.Version ;
namespace GenerateInnoSetupScript.BusinessProcess
{
  public class GenScriptBusinessProcess
  {
    private string TemplatePath { get ; }
    private Config Config { get ; }
    public string OutputFilePath { get ; }

    public GenScriptBusinessProcess( ApplicationParameters parameters, Config config )
    {
      if ( ! File.Exists( parameters.TemplatePath ) ) {
        throw new FileNotFoundException( $"Not found template file with path \"{parameters.TemplatePath}\"." ) ;
      }
      TemplatePath = parameters.TemplatePath ;
      Config = config ;
      OutputFilePath = $"{parameters.OutputPath}\\{config.AppInfo.AppName}.iss".RemoveUnnecessaryBackslash() ;
    }

    private string GetTemplateContent()
    {
      using var r = new StreamReader( TemplatePath ) ;
      return r.ReadToEnd() ;
    }

    private void SaveFile( string outputFilePath, string content )
    {
      using var w = new StreamWriter( outputFilePath, false, Encoding.UTF8 ) ;
      w.Write( content ) ;
    }

    private string GenerateScript()
    {
      var templateString = GetTemplateContent() ;
      var templateContext = MakeTemplateContext() ;

      var parser = new FluidParser() ;
      if ( parser.TryParse( templateString, out var template, out var error ) ) {
        return template.Render( templateContext ) ;
      }

      throw new MyAppErrorException( "Generated Script", error ) ;
    }

    private TemplateContext MakeTemplateContext()
    {
      var options = new TemplateOptions() ;
      options.MemberAccessStrategy.Register<Version>() ;
      options.MemberAccessStrategy.Register<AppInfo>() ;
      options.MemberAccessStrategy.Register<Module>() ;

      foreach ( var module in Config.Modules ) {
        module.ResourceFolders = Config.Versions.ToDictionary(
          version => version.Name,
          version =>
          {
            return new DirectoryInfo( module.SrcPath + version.SourcePath )
              .EnumerateDirectories()
              .Where( dir => dir.EnumerateFileSystemInfos().Any() )
              .Select( x => x.Name + "\\" )
              .ToList() ;
          } ) ;
      }
      return new TemplateContext( Config, options ) ;
    }


    public void Run()
    {
      SaveFile( OutputFilePath, GenerateScript() ) ;
    }
  }
}