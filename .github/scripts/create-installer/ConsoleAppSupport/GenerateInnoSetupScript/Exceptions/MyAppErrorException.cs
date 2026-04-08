using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.Serialization ;
namespace GenerateInnoSetupScript.Exceptions
{
  [Serializable]
  public class MyAppErrorException : Exception
  {
    public List<string> ErrorMessages { get ; set ; }
    public string Title { get ; set ; }

    public MyAppErrorException()
    {
    }
    public MyAppErrorException( string message ) : base( message )
    {
    }
    public MyAppErrorException( string message, Exception innerException ) : base( message, innerException )
    {
    }
    protected MyAppErrorException( SerializationInfo info, StreamingContext context ) : base( info, context )
    {
    }

    public MyAppErrorException( string title, List<string> errorMessages) : base(string.Join( Environment.NewLine, errorMessages!.Select( x => "- " + x ) ))
    {
      Title = title ;
      ErrorMessages = errorMessages ;
    }
    
    public MyAppErrorException( string title, params string[] errorMessages) : base(string.Join( Environment.NewLine, errorMessages!.Select( x => "- " + x ) ))
    {
      Title = title ;
      ErrorMessages = errorMessages.ToList() ;
    }

  }
}