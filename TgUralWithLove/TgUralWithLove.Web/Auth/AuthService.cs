
using System.Security.Cryptography;
using System.Text;

namespace TgUralWithLove.Web.Auth
{
    public class AuthService
    {
        IConfiguration configuration;
        string BotToken;
        List<long> Admins;
        string AdminLogin, AdminPassword;
        string CustomerLogin, CustomerPassword;
        ILogger logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            BotToken = configuration.GetSection("Telegram")["Token"];
            Admins = configuration.GetSection("Auth")["TgAdminsList"]
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => long.Parse(x))
                .ToList();
            AdminLogin = configuration.GetSection("Auth")["LocalAdminLogin"];
            AdminPassword = configuration.GetSection("Auth")["LocalAdminPassword"];
            CustomerLogin = configuration.GetSection("Auth")["LocalCustomerLogin"];
            CustomerPassword = configuration.GetSection("Auth")["LocalCustomerPassword"];
        }

        public async Task<CustomUser?> ValidateUserAsync(string username, string password)
        {
            // Здесь ваша логика проверки учетных данных, например, через API или базу данных
            if (username == AdminLogin && password == AdminPassword)
            {
                logger.LogInformation($"LoggedUserInfo: Admin; {username}");
                return new CustomUser
                {
                    Username = username,
                    FirstName = username,
                    Role = "Admin",
                    IsAuthenticated = true,
                    Id = 1
                };
            }
            if (username == CustomerLogin && password == CustomerPassword)
            {
                logger.LogInformation($"LoggedUserInfo: Admin; {username}");
                return new CustomUser
                {
                    Username = username,
                    FirstName = username,
                    Role = "Customer",
                    IsAuthenticated = true,
                    Id = 2
                };
            }

            return null; // Если проверка не удалась
        }

        public async Task<CustomUser?> ValidateUserAsync(TgUserData tgUser)
        {
            List<string> vales = new List<string>();
            if (tgUser.id.HasValue)
                vales.Add($"id={tgUser.id}");
            if (tgUser.username != null)
                vales.Add($"username={tgUser.username}");
            if (tgUser.auth_date != null)
                vales.Add($"auth_date={tgUser.auth_date}");
            if (tgUser.first_name != null)
                vales.Add($"first_name={tgUser.first_name}");
            if (tgUser.last_name != null)
                vales.Add($"last_name={tgUser.last_name}");
            if (tgUser.photo_url != null)
                vales.Add($"photo_url={tgUser.photo_url}");

            vales.Sort();
            string data_check_string = string.Join("\n", vales);

            byte[] secretKey = ComputeSHA256(BotToken);

            CustomUser cu = null;

            if (VerifyTelegramData(secretKey, data_check_string, tgUser.hash))
            {
                cu = new CustomUser
                {
                    Username = tgUser.username,
                    Role = "Customer",
                    IsAuthenticated = true,
                    FirstName = tgUser.first_name,
                    LastName = tgUser.last_name,
                    PhotoUrl = tgUser.photo_url,
                    Id = tgUser.id,
                };
                if (tgUser.id.HasValue && Admins.Contains(tgUser.id.Value))
                    cu.Role = "Admin";

                logger.LogInformation($"LoggedUserInfo: {cu.Role}; {data_check_string}");
            }
            return cu;
        }


        byte[] ComputeSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        bool VerifyTelegramData(byte[] secretKey, string dataCheckString, string hash)
        {
            using (HMACSHA256 hmac = new HMACSHA256(secretKey))
            {
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
                string computedHashHex = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

                return computedHashHex == hash;
            }
        }
    }

}
