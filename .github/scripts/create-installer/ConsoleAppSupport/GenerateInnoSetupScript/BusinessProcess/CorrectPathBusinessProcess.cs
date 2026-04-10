using System.Collections.Generic ;
using System.IO ;
using System.Text ;
using System.Xml ;
using System.Xml.Serialization ;
using GenerateInnoSetupScript.Exceptions ;
using GenerateInnoSetupScript.Extensions ;
using GenerateInnoSetupScript.Model.CorrectPath ;
using GenerateInnoSetupScript.Model.GenCode ;
namespace GenerateInnoSetupScript.BusinessProcess
{
  public class CorrectPathBusinessProcess
  {
    public Config Config { get ; set ; }

    public CorrectPathBusinessProcess( Config config )
    {
      Config = config ;
    }

    public void Run()
    {
      List<string> errors = new List<string>() ;

      foreach ( var version in Config.Versions ) {
        foreach ( var module in Config.Modules ) {
          // Correct path in .addin file
          string basePath = $"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{version.Name}\\{module.DestPath}".RemoveUnnecessaryBackslash() ;
          var fi = new FileInfo( $"{module.SrcPath}{version.SourcePath}{module.Name}.addin" ) ;
          if ( fi.Exists ) {
            HandleFor( fi, basePath ) ;
          }
          else {
            errors.Add( $"File not found: {fi.FullName}" ) ;
          }
        }
      }

      if ( errors.Count > 0 ) {
        throw new MyAppErrorException( "Correct Path", errors ) ;
      }
    }

    private void HandleFor( FileInfo addInFileInfo, string correctPath )
    {
      var serializer = new XmlSerializer( typeof( RevitAddIns ) ) ;
      var revitAddIns = ReadAddInFile( addInFileInfo, serializer ) ;
      foreach ( var addIn in revitAddIns.AddIns ) {
        var fi = new FileInfo( addIn.Assembly ) ;
        addIn.Assembly = $"{correctPath}\\{fi.Name}".RemoveUnnecessaryBackslash() ;
      }

      UpdateAddInFile( addInFileInfo, revitAddIns, serializer ) ;
    }

    private RevitAddIns ReadAddInFile( FileInfo fileInfo, XmlSerializer serializer )
    {
      using ( var reader = fileInfo.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) ) {
        var revitAddIns = (RevitAddIns) serializer.Deserialize( reader ) ;
        return revitAddIns ;
      }
    }

    private void UpdateAddInFile( FileInfo fileInfo, RevitAddIns revitAddIns, XmlSerializer serializer )
    {
      using ( var writter = fileInfo.Open( FileMode.Create, FileAccess.Write, FileShare.Write ) ) {
        XmlTextWriter xmlWriter = new XmlTextWriter( writter, Encoding.UTF8 ) ;
        xmlWriter.Formatting = Formatting.Indented ;
        xmlWriter.Indentation = 2 ;
        XmlSerializerNamespaces ns = new XmlSerializerNamespaces() ;
        ns.Add( "", "" ) ;
        serializer.Serialize( xmlWriter, revitAddIns, ns ) ;
      }
    }
  }
}