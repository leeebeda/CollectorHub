using CollectorHub.Models;
using Microsoft.EntityFrameworkCore;

namespace CollectorHub.Services
{
    public class AuthService
    {
        private readonly DBContext _context;

        public AuthService(DBContext context)
        {
            _context = context;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == email);

            if (user == null)
                return null;

            // Проверяем пароль
            if (!VerifyPassword(password, user.password_hash))
                return null;

            return user;
        }

        public async Task<User> RegisterAsync(string email, string username, string password)
        {
            // Проверяем, что пользователь с таким email не существует
            if (await _context.Users.AnyAsync(u => u.email == email))
                throw new Exception("Пользователь с таким email уже существует");

            // Создаем нового пользователя
            var user = new User
            {
                email = email,
                username = username,
                password_hash = HashPassword(password),
                role_id = 2 // ID роли обычного пользователя
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private string HashPassword(string password)
        {
            // Используем BCrypt для хеширования пароля
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            // Проверка хеша пароля
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}