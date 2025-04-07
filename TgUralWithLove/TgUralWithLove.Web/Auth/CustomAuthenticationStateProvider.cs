using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace TgUralWithLove.Web.Auth
{

    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        public const string DeviceCookieKey = "DeviceKey";
        private readonly AuthService _authService;

        public CustomUser? CurrentUser
        {
            get
            {
                var key = contextAccessor.HttpContext?.Request.Cookies[DeviceCookieKey];
                if(key != null)
                    return memoryCache.Get<CustomUser>($"LoggedUser_{key}");
                return null;
            }
            private set
            {
                var key = contextAccessor.HttpContext?.Request.Cookies[DeviceCookieKey];
                if(key == null || value == null)
                    memoryCache.Remove($"LoggedUser_{key}");
                else
                    memoryCache.Set($"LoggedUser_{key}", value, TimeSpan.FromDays(7));
                
            }

        }

        IHttpContextAccessor contextAccessor;
        IMemoryCache memoryCache;



        public CustomAuthenticationStateProvider(AuthService authService, IHttpContextAccessor contextAccessor, IMemoryCache memoryCache)
        {
            _authService = authService;
            this.contextAccessor = contextAccessor;
            this.memoryCache = memoryCache;
        }

        public async Task<bool> SignInAsync(string username, string password)
        {
            var user = await _authService.ValidateUserAsync(username, password);
            if (user != null)
            {
                CurrentUser = user;


                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());



                return true; // Успешная аутентификация
            }

            return false; // Неверные учетные данные
        }

        public async Task<bool> SignInAsync(TgUserData tgUser)
        {
            var user = await _authService.ValidateUserAsync(tgUser);
            if (user != null)
            {
                CurrentUser = user;
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true; // Успешная аутентификация
            }

            return false; // Неверные учетные данные
        }

        public void SignOut()
        {
            CurrentUser = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimsIdentity identity;

            if (CurrentUser != null && CurrentUser.IsAuthenticated)
            {
                identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, CurrentUser?.Id?.ToString()),
                        new Claim(ClaimTypes.Name, CurrentUser.Username),
                        new Claim(ClaimTypes.Role, CurrentUser.Role)
                    }
                    , "CustomAuth"
                );
            }
            else
            {
                identity = new ClaimsIdentity(); // Неаутентифицированный пользователь
            }

            var claimsPrincipal = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
    }

}
