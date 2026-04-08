using System ;
using System.Diagnostics ;
using GenerateInnoSetupScript.Exceptions ;
namespace GenerateInnoSetupScript.BusinessProcess
{
  public class CompileScriptBusinessProcess 
  {
    private int ExitCode { get ; set ; }
    public string ScriptPath { get ; set ; }

    public CompileScriptBusinessProcess( string scriptPath )
    {
      ScriptPath = $"/Sbyparam=$p \"{scriptPath}\"" ;
    }

    public void Run( )
    {
      var process = new Process() ;
      process.StartInfo.FileName = "ISCC" ;
      process.StartInfo.Arguments = ScriptPath ;
      process.StartInfo.RedirectStandardError = true ;
      process.StartInfo.UseShellExecute = false ;
      process.EnableRaisingEvents = true ;
      process.Exited += ProcessEnded ;
      process.Start() ;
      process.WaitForExit() ;

      if ( ExitCode != 0 ) {
        throw new MyAppErrorException( "COMPILE SCRIPT",  process.StandardError.ReadToEnd() ) ;
      }
    }

    private void ProcessEnded( object sender, EventArgs e )
    {
      if ( sender is Process process ) {
        ExitCode = process.ExitCode ;
      }
    }

  }
}