using System ;
using System.Diagnostics ;
using GenerateInnoSetupScript.Exceptions ;
using GenerateInnoSetupScript.Model ;
using GenerateInnoSetupScript.Model.GenCode ;
namespace GenerateInnoSetupScript.BusinessProcess
{
  public class BuildISOBusinessProcess
  {
    private Config _config ;
    private ApplicationParameters _parameters ;
    private int _exitCode { get ; set ; }

    public BuildISOBusinessProcess(Config config, ApplicationParameters parameters)
    {
      _config = config ;
      _parameters = parameters ;
    }

    public void Run()
    {
      var outputPath = $"{_parameters.OutputPath}/Output" ;
      var exePath = $"{outputPath}/{_config.AppInfo.OutputFilename}.exe" ;
      var isoPath = $"{outputPath}/{_config.AppInfo.OutputFilename}.iso" ;

      var process = new Process() ;
      process.StartInfo.FileName = _config.AppInfo.CustomConfig["ImgBurn"] ;
      process.StartInfo.Arguments = $"/MODE BUILD /BUILDMODE IMAGEFILE /FILESYSTEM \"ISO9660 + UDF\" /UDFVERSION \"1.02\"/ROOTFOLDER NO /VOLUMELABEL \"LIGHTNING_BIM\" /OVERWRITE YES /CLOSE /NOIMAGEDETAILS /START /SRC \"{exePath}\" /DEST \"{isoPath}\"" ;
      process.StartInfo.RedirectStandardError = true ;
      process.StartInfo.UseShellExecute = false ;
      process.EnableRaisingEvents = true ;
      process.Exited += ProcessEnded ;
      process.Start() ;
      process.WaitForExit() ;
      
      if ( _exitCode != 0 ) {
        throw new MyAppErrorException( "BUILD ISO IMAGE FILE FAILED",  process.StandardError.ReadToEnd() ) ;
      }
    }
    
    private void ProcessEnded( object sender, EventArgs e )
    {
      if ( sender is Process process ) {
        _exitCode = process.ExitCode ;
      }
    }

  }
}