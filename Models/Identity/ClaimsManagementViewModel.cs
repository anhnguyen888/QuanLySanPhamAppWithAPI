using System.Collections.Generic;

namespace QuanLySanPhamApp.Models.Identity;

public class ClaimsManagementViewModel
{
    public string UserId { get; set; } = string.Empty;
    public List<UserClaimViewModel> Claims { get; set; } = new List<UserClaimViewModel>();
    
    public string NewClaimType { get; set; } = string.Empty;
    public string NewClaimValue { get; set; } = string.Empty;
}

public class UserClaimViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Delete { get; set; }
}
