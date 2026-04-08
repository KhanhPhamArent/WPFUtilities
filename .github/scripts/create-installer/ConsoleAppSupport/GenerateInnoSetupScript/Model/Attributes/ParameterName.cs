using System ;
namespace GenerateInnoSetupScript.Model.Attributes
{
  [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
  public class ParameterNameAttribute : Attribute
  {
    public string Name { get ; }

    public ParameterNameAttribute( string name )
    {
      Name = name ;
    }
  }
}