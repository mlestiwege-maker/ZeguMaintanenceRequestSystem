namespace ZEGU.WebApp.ViewModels.Account
{
    public class TwoFactorAuthenticationViewModel
    {
        public bool Is2faEnabled { get; set; }
        public bool HasAuthenticator { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }

    public class EnableAuthenticatorViewModel
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
        public string QrCodeImageBase64 { get; set; } = string.Empty;
    }
}
