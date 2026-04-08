using System.Text.RegularExpressions ;
namespace GenerateInnoSetupScript.Extensions
{
  public static class StringExtensions
  {
    public static string DefaultIfBlank( this string s, string defaultValue ) => string.IsNullOrWhiteSpace( s ) ? defaultValue : s ;
    public static string RemoveUnnecessaryBackslash( this string s )
    {
      if ( string.IsNullOrWhiteSpace( s ) ) return s ;

      return Regex.Replace( s, @"\\+", @"\" ) ;

    }
  }
}