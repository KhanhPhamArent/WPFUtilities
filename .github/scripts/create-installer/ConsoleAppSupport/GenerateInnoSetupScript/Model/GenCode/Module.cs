using System.Collections.Generic ;
namespace GenerateInnoSetupScript.Model.GenCode
{
  public class Module
  {
    public string Name { get ; set ; }
    public string SrcPath { get ; set ; }
    public string DestPath { get ; set ; }

    public Dictionary<string, List<string>> ResourceFolders { get ; set ; }

    public Dictionary<string, string> CustomConfig { get ; set ; }

  }
}