namespace QuanLySanPhamApp.Models.ManageViewModels
{
    public class TwoFactorAuthenticationViewModel
    {
        public bool HasAuthenticator { get; set; }

        public int RecoveryCodesLeft { get; set; }

        public bool Is2faEnabled { get; set; }

        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }
    }
}
