using System ;
using System.Collections.Generic ;
using GenerateInnoSetupScript.Extensions ;
namespace GenerateInnoSetupScript.Model.GenCode
{
  public class AppInfo
  {
    private string _outputFilename ;
    private string _appPublisher ;
    private string _appUrl ;
    private string _appExeName ;
    private string _appLicenseName ;
    private string _appLicenseTxt ;
    private string _appManualName ;
    private string _appManualPdf ;
    private string _appAssocExt ;
    private string _appAssocName ;
    private string _appAssocKey ;
    private string _appVersion ;

    public string AppId { get ; set ; }
    public string AppName { get ; set ; }
    public string AppVersion
    {
      get => _appVersion ;
      set => _appVersion = $"{value}" ;
    }

    public bool IsIncludedAppExe { get ; set ; }
    public string AppExeName
    {
      get
      {
        if ( IsIncludedAppExe && string.IsNullOrWhiteSpace( _appExeName ) ) {
          throw new InvalidOperationException( "AppExeName has not been configured before" ) ;
        }
        return _appExeName ;
      }
      set => _appExeName = value ;
    }

    public bool IsIncludedLicense { get ; set ; }
    public string AppLicenseName
    {
      get
      {
        if ( IsIncludedLicense && string.IsNullOrWhiteSpace( _appLicenseName ) ) {
          throw new InvalidOperationException( "AppLicenseName has not been configured before" ) ;
        }
        return _appLicenseName ;
      }
      set => _appLicenseName = value ;
    }
    public string AppLicenseTxt
    {
      get
      {
        if ( IsIncludedLicense && string.IsNullOrWhiteSpace( _appLicenseTxt ) ) {
          throw new InvalidOperationException( "AppLicenseTxt has not been configured before" ) ;
        }
        return _appLicenseTxt ;
      }
      set => _appLicenseTxt = value ;
    }

    public bool IsIncludedManual { get ; set ; }
    public string AppManualName
    {
      get
      {
        if ( IsIncludedManual && string.IsNullOrWhiteSpace( _appManualName ) ) {
          throw new InvalidOperationException( "AppManualName has not been configured before" ) ;
        }
        return _appManualName ;
      }
      set => _appManualName = value ;
    }
    public string AppManualPdf
    {
      get
      {
        if ( IsIncludedManual && string.IsNullOrWhiteSpace( _appManualPdf ) ) {
          throw new InvalidOperationException( "AppManualPdf has not been configured before" ) ;
        }
        return _appManualPdf ;
      }
      set => _appManualPdf = value ;
    }

    public string CopyRight { get ; set ; }

    public string AppAssocName
    {
      get => _appAssocName.DefaultIfBlank( AppName + " File" ) ;
      set => _appAssocName = value ;
    }
    public string AppAssocExt
    {
      get => _appAssocExt.DefaultIfBlank( ".myp" ) ;
      set => _appAssocExt = value ;
    }
    public string AppAssocKey
    {
      get => _appAssocKey.DefaultIfBlank( AppAssocName.Replace( " ", "" ) + AppAssocExt ) ;
      set => _appAssocKey = value ;
    }

    public string AppPublisher
    {
      get => _appPublisher.DefaultIfBlank( "Arent Inc." ) ;
      set => _appPublisher = value ;
    }
    public string AppUrl
    {
      get => _appUrl.DefaultIfBlank( "https://arent3d.com" ) ;
      set => _appUrl = value ;
    }
    public string OutputFilename { get ; set ; } = "";

    public Dictionary<string, string> CustomConfig { get ; set ; }
  }
}