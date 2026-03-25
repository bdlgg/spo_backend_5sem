using System.ComponentModel.DataAnnotations;

namespace backend_spo.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неккоректный формат email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(1, ErrorMessage = "Пароль не может быть пустым")]
    public string Password { get; set; } = string.Empty;
}