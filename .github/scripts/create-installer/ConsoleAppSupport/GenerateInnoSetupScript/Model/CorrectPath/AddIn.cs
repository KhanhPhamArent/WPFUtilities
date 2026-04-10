using System.Xml.Serialization ;
namespace GenerateInnoSetupScript.Model.CorrectPath
{
  /// <summary>
  /// 
  /// </summary>
  public class AddIn
  {

    [XmlElement]
    public string Text { get ; set ; }

    [XmlElement]
    public string LongDescription { get ; set ; }

    [XmlElement]
    public string ClientId { get ; set ; }

    [XmlElement]
    public string Name { get ; set ; }

    [XmlElement]
    public string AddInId { get ; set ; }

    [XmlElement]
    public string FullClassName { get ; set ; }

    [XmlElement]
    public string Assembly { get ; set ; }

    [XmlElement]
    public string VendorId { get ; set ; }

    [XmlElement]
    public string VendorDescription { get ; set ; }

    [XmlAttribute]
    public string Type { get ; set ; }
  }
}