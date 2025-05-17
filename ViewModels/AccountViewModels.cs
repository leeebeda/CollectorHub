using System.ComponentModel.DataAnnotations;

namespace CollectorHub.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, ErrorMessage = "Имя пользователя должно быть от {2} до {1} символов", MinimumLength = 3)]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, ErrorMessage = "Пароль должен быть от {2} до {1} символов", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}