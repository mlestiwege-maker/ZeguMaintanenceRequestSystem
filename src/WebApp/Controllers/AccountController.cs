using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using QRCoder;
using System.Text;
using System.Text.Encodings.Web;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;
using ZEGU.WebApp.Services;
using ZEGU.WebApp.ViewModels.Account;

namespace ZEGU.WebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DataExportService _dataExportService;

        private string? CurrentUserName => User.Identity?.Name;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, DataExportService dataExportService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _dataExportService = dataExportService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please enter username and password");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, rememberMe, false);

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                    await _signInManager.SignInAsync(user, rememberMe);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid username or password");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RememberMe"] = rememberMe;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(string code, bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var authenticatorCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, rememberClient: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked out due to too many failed attempts.");
                return View();
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RememberMe"] = rememberMe;
            ModelState.AddModelError("", "Invalid authenticator code.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string recoveryCode, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode.Replace(" ", string.Empty));

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked out due to too many failed attempts.");
                return View();
            }

            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError("", "Invalid recovery code.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string firstName, string lastName, string email, string username, string password, string confirmPassword, UserRole role, string? studentNumber = null, string? staffNumber = null)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View();
            }

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = username,
                Role = role,
                StudentNumber = studentNumber,
                StaffNumber = staffNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role.ToString());
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(string firstName, string lastName, string email, string? phoneNumber = null)
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.UserName = email;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match");
                return View();
            }

            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportMyData()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var data = await _dataExportService.ExportUserDataAsync(user.Id);
            return File(data, "application/json", $"my-data-{DateTime.UtcNow:yyyyMMdd}.json");
        }

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed(string password)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                ModelState.AddModelError("", "Password is incorrect");
                return View("DeleteAccount");
            }

            var result = await _dataExportService.AnonymizeUserDataAsync(user.Id);
            if (result)
            {
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "Your account has been anonymized successfully.";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Failed to delete account. Please contact support.");
            return View("DeleteAccount");
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var model = new TwoFactorAuthenticationViewModel
            {
                Is2faEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
                HasAuthenticator = !string.IsNullOrEmpty(await _userManager.GetAuthenticatorKeyAsync(user))
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var model = BuildEnableAuthenticatorViewModel(user, unformattedKey!);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(string code)
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var verificationCode = (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!isValid)
            {
                var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                var model = BuildEnableAuthenticatorViewModel(user, unformattedKey!);
                ModelState.AddModelError("", "Verification code is invalid.");
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return View("ShowRecoveryCodes", recoveryCodes.ToArray());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);

            TempData["SuccessMessage"] = "Two-factor authentication has been disabled. You'll need to set it up again to re-enable it.";
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (!user.TwoFactorEnabled)
            {
                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return View("ShowRecoveryCodes", recoveryCodes.ToArray());
        }

        private EnableAuthenticatorViewModel BuildEnableAuthenticatorViewModel(ApplicationUser user, string unformattedKey)
        {
            const string issuer = "ZEGU Maintenance";
            var email = user.Email ?? user.UserName ?? "user";
            var authenticatorUri = string.Format(
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
                UrlEncoder.Default.Encode(issuer),
                UrlEncoder.Default.Encode(email),
                unformattedKey);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);

            return new EnableAuthenticatorViewModel
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = authenticatorUri,
                QrCodeImageBase64 = Convert.ToBase64String(qrCodeBytes)
            };
        }

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var position = 0;
            while (position + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(position, 4)).Append(' ');
                position += 4;
            }
            if (position < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(position));
            }
            return result.ToString().ToUpperInvariant();
        }
    }
}
