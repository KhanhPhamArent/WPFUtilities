using System.Collections.Generic ;
using System.Xml.Serialization ;
namespace GenerateInnoSetupScript.Model.CorrectPath
{
  [XmlRoot( "RevitAddIns" )]
  public class RevitAddIns
  {
    [XmlElement( "AddIn" )]
    public List<AddIn> AddIns { get ; set ; }
  }
}