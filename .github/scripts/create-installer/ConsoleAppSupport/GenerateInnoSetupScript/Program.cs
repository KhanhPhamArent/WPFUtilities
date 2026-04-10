using System ;
using System.Collections.Generic ;
using System.Threading ;
using GenerateInnoSetupScript.BusinessProcess ;
using GenerateInnoSetupScript.Exceptions ;
using GenerateInnoSetupScript.Model ;
using GenerateInnoSetupScript.Model.GenCode ;
namespace GenerateInnoSetupScript
{
  public static class Program
  {
    static int Main( string[] args )
    {
      try {
        ApplicationParameters parameters = new ApplicationParameters( args ) ;
        var config = Config.LoadConfigFromFile( parameters.ConfigPath ) ;

        // Auto correct path in .addin file
        new CorrectPathBusinessProcess( config ).Run() ;

        // Gen script inno setup
        var genScriptBusinessProcess = new GenScriptBusinessProcess( parameters, config );
        genScriptBusinessProcess.Run();

        // Build
        new CompileScriptBusinessProcess( genScriptBusinessProcess.OutputFilePath ).Run() ;
        
        // Build ISO file
        if ( config.AppInfo.CustomConfig != null && config.AppInfo.CustomConfig.ContainsKey( "ImgBurn" ) ) {
          new BuildISOBusinessProcess( config, parameters ).Run() ;
        }

        ShowSuccessMessage() ;
        return 0 ;
      }
      catch ( MyAppErrorException appErrorException ) {
        ShowErrorMessage( appErrorException.Title, appErrorException.ErrorMessages ) ;
        return 1 ;
      }
      catch ( Exception e ) {
        ShowErrorMessage( e.Message, new List<string>() { e.StackTrace } ) ;
        return 1 ;
      }
    }

    static void ShowErrorMessage( string title, List<string> errors )
    {
      Console.ResetColor() ;
      Console.WriteLine( $"--------------------------------" ) ;
      Console.ForegroundColor = ConsoleColor.Red ;
      Console.WriteLine( $"{title.ToUpper()} FAILURE" ) ;
      Console.WriteLine( $"Total errors: {errors.Count} error(s)" ) ;
      Console.WriteLine() ;
      Console.WriteLine( $"-------- DETAILS " ) ;
      foreach ( var item in errors ) {
        Console.ForegroundColor = ConsoleColor.Red ;
        Console.Error.WriteLine( "- " + item ) ;
      }
      Console.ResetColor() ;
      Console.WriteLine( $"-------------------------------------------" ) ;
      Console.WriteLine( "The program will close after 5 seconds . . ." ) ;
      Thread.Sleep(5000) ;
      Console.WriteLine( $"-------------------------------------------" ) ;
    }

    static void ShowSuccessMessage()
    {
      Console.ResetColor() ;
      Console.WriteLine( $"--------------------------------" ) ;
      Console.ForegroundColor = ConsoleColor.Green ;
      Console.WriteLine( $"SUCCEED" ) ;
      Console.ResetColor() ;
      Console.WriteLine( $"--------------------------------" ) ;
    }

  }
}