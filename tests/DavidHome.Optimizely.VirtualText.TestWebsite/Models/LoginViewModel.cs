using System.ComponentModel.DataAnnotations;

namespace DavidHome.Optimizely.VirtualText.TestWebsite.Models;

public class LoginViewModel
{
    [Required] public string Username { get; set; }

    [Required] public string Password { get; set; }
}