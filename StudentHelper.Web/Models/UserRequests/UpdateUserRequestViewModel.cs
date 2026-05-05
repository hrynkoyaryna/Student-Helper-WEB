using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.UserRequests;

public class UpdateUserRequestStatusViewModel
{
    public int Id { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? AdminResponse { get; set; }
}