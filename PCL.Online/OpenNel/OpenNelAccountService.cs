using System;
using System.Collections;
using System.Linq;
using OpenNEL.Auth;
using OpenNEL.Entities.Web;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Handlers.PC.Account;
using OpenNEL.Handlers.PC.Login;
using OpenNEL.Manager;

namespace PCL.Online.OpenNel;

public static class OpenNelAccountService
{
    public static OpenNelAccountLoginResult Login4399Password(string account, string password,
        string? captchaSessionId = null, string? captcha = null, string? displayName = null)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new Login4399().Execute(account, password, captchaSessionId, captcha);
        return ParseLoginResult(result, displayName ?? account, OpenNelProvider.Game4399,
            OpenNelLoginKind.Login4399Password);
    }

    public static OpenNelAccountLoginResult LoginNeteaseCookie(string cookie, string? displayName = null)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new LoginCookie().Execute(cookie);
        return ParseLoginResult(result, displayName ?? "Netease Cookie", OpenNelProvider.Netease,
            OpenNelLoginKind.NeteaseCookie);
    }

    public static OpenNelActionResult SendNeteaseSmsCode(string phone)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new LoginPhone().Execute(phone).GetAwaiter().GetResult();
        return ParseActionResult(result, "sms_sent", "验证码已发送");
    }

    public static OpenNelAccountLoginResult LoginNeteasePhone(string phone, string code, string? displayName = null)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new LoginPhone().ExecuteVerify(phone, code).GetAwaiter().GetResult();
        return ParseLoginResult(result, displayName ?? phone, OpenNelProvider.Netease,
            OpenNelLoginKind.NeteasePhone);
    }

    public static OpenNelAccountLoginResult LoginNeteaseEmail(string email, string password, string? displayName = null)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new LoginNeteaseEmail().Execute(email, password).GetAwaiter().GetResult();
        return ParseLoginResult(result, displayName ?? email, OpenNelProvider.Netease,
            OpenNelLoginKind.NeteaseEmail);
    }

    public static OpenNelAccountLoginResult Activate(string entityId, string? displayName = null,
        OpenNelProvider provider = OpenNelProvider.Unknown, OpenNelLoginKind loginKind = OpenNelLoginKind.Unknown)
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new ActivateAccount().Execute(entityId);
        return ParseLoginResult(result, displayName ?? entityId, provider, loginKind);
    }

    public static OpenNelAccountLoginResult ReloginWithStoredDetails(OpenNelLoginKind loginKind, string detailsJson,
        string displayName)
    {
        OpenNelRuntime.EnsureInitialized();
        return loginKind switch
        {
            OpenNelLoginKind.Login4399Password => Relogin4399(detailsJson, displayName),
            OpenNelLoginKind.NeteaseCookie => LoginNeteaseCookie(detailsJson, displayName),
            OpenNelLoginKind.NeteaseEmail => ReloginNeteaseEmail(detailsJson, displayName),
            OpenNelLoginKind.NeteasePhone => ReloginNeteasePhone(detailsJson, displayName),
            _ => new OpenNelAccountLoginResult(false, "不支持的社区登录类型", Code: "unsupported_login_kind")
        };
    }

    private static OpenNelAccountLoginResult Relogin4399(string detailsJson, string displayName)
    {
        EntityPasswordRequest? request = System.Text.Json.JsonSerializer.Deserialize<EntityPasswordRequest>(detailsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (request is null || string.IsNullOrWhiteSpace(request.Account) || string.IsNullOrWhiteSpace(request.Password))
            return new OpenNelAccountLoginResult(false, "无法解析 4399 登录信息", Code: "invalid_4399_details");

        return Login4399Password(request.Account, request.Password, displayName: displayName);
    }

    private static OpenNelAccountLoginResult ReloginNeteaseEmail(string detailsJson, string displayName)
    {
        NeteasePersistedLogin? persisted = NeteasePersistedLoginParser.Parse(detailsJson, "email");
        if (persisted is null || string.IsNullOrWhiteSpace(persisted.Email))
            return new OpenNelAccountLoginResult(false, "无法解析 Netease 邮箱登录信息", Code: "invalid_netease_email");
        if (string.IsNullOrWhiteSpace(persisted.Password))
            return new OpenNelAccountLoginResult(false, "Netease 邮箱会话缺少密码，无法重新登录", Code: "netease_email_password_missing");

        return LoginNeteaseEmail(persisted.Email, persisted.Password, displayName);
    }

    private static OpenNelAccountLoginResult ReloginNeteasePhone(string detailsJson, string displayName)
    {
        NeteasePersistedLogin? persisted = NeteasePersistedLoginParser.Parse(detailsJson, "phone");
        if (persisted is null || string.IsNullOrWhiteSpace(persisted.Phone))
            return new OpenNelAccountLoginResult(false, "无法解析 Netease 手机登录信息", Code: "invalid_netease_phone");
        if (!persisted.HasReusableSession)
        {
            return new OpenNelAccountLoginResult(false, "手机号登录已过期，请重新获取验证码登录",
                Code: "phone_relogin_required", Phone: persisted.Phone);
        }

        using var auth = new NeteaseDirectAuthService();
        var result = auth.RestoreWithSessionAsync(new NeteaseSessionSnapshot
        {
            SessionId = persisted.SessionId!,
            SdkUid = persisted.SdkUid!,
            LoginChannel = persisted.LoginChannel!,
            DeviceId = persisted.DeviceId!
        }).GetAwaiter().GetResult();

        if (!result.Success || result.Data is null)
        {
            return new OpenNelAccountLoginResult(false, result.Message, Code: "phone_relogin_required",
                Phone: persisted.Phone);
        }

        string entityId = result.Data.EntityId;
        UserManager.Instance.AddUserToMaintain(new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp
        {
            EntityId = entityId,
            AccessToken = result.Data.AccessToken,
            Token = result.Data.Token,
            SdkUid = result.Data.Session.SdkUid,
            Account = persisted.Phone
        });
        EntityUser? existingUser = UserManager.Instance.GetUserByEntityId(entityId);
        if (existingUser is null)
        {
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = entityId,
                Authorized = true,
                AutoLogin = false,
                Channel = result.Data.LoginChannel,
                Type = "phone",
                Details = detailsJson
            });
        }
        else
        {
            existingUser.Authorized = true;
            existingUser.Channel = result.Data.LoginChannel;
            existingUser.Details = detailsJson;
            UserManager.Instance.AddUser(existingUser);
        }
        UserManager.Instance.SaveUsersToDisk();

        return BuildSucceededLogin(entityId, displayName, OpenNelProvider.Netease, OpenNelLoginKind.NeteasePhone);
    }

    private static OpenNelAccountLoginResult ParseLoginResult(object? result, string displayName, OpenNelProvider provider,
        OpenNelLoginKind loginKind)
    {
        if (result is null)
            return new OpenNelAccountLoginResult(false, "登录失败", Code: "null_result");

        string type = GetProperty(result, "type") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (type.StartsWith("captcha_required", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenNelAccountLoginResult(false, "4399 登录需要验证码", Code: type,
                    CaptchaUrl: GetProperty(result, "captchaUrl"),
                    SessionId: GetProperty(result, "sessionId"),
                    AccountName: GetProperty(result, "account"),
                    Password: GetProperty(result, "password"));
            }

            if (type.Equals("phone_relogin_required", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenNelAccountLoginResult(false,
                    GetProperty(result, "message") ?? "手机号登录已过期，请重新获取验证码登录",
                    Code: type,
                    EntityId: GetProperty(result, "entityId"),
                    Phone: GetProperty(result, "phone"));
            }

            if (type.EndsWith("_error", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("login_error", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenNelAccountLoginResult(false, GetProperty(result, "message") ?? "登录失败", Code: type);
            }

            if (type.Equals("login_x19_verify", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenNelAccountLoginResult(false, "需要完成安全验证", Code: type,
                    VerifyUrl: GetProperty(result, "verify_url"));
            }
        }

        if (!TryGetSucceededEntityId(result, out string entityId))
            return new OpenNelAccountLoginResult(false, GetProperty(result, "message") ?? "登录失败", Code: type);

        return BuildSucceededLogin(entityId, displayName, provider, loginKind);
    }

    private static OpenNelAccountLoginResult BuildSucceededLogin(string entityId, string displayName,
        OpenNelProvider provider, OpenNelLoginKind loginKind)
    {
        EntityUser? user = UserManager.Instance.GetUserByEntityId(entityId);
        var available = UserManager.Instance.GetAvailableUser(entityId);

        OpenNelLoginKind resolvedKind = loginKind != OpenNelLoginKind.Unknown
            ? loginKind
            : InferLoginKind(user?.Type);
        OpenNelProvider resolvedProvider = provider != OpenNelProvider.Unknown
            ? provider
            : InferProvider(resolvedKind);

        string effectiveName = string.IsNullOrWhiteSpace(displayName)
            ? user?.Alias ?? user?.UserId ?? entityId
            : displayName;

        var account = new OpenNelAccountResult(
            Provider: resolvedProvider,
            LoginKind: resolvedKind,
            EntityId: entityId,
            DisplayName: effectiveName,
            AccessToken: available?.AccessToken ?? string.Empty,
            LoginChannel: user?.Channel ?? string.Empty,
            PersistedDetailsJson: user?.Details ?? string.Empty);

        return new OpenNelAccountLoginResult(true, "登录成功", account, Code: "success");
    }

    private static OpenNelActionResult ParseActionResult(object? result, string successType, string successMessage)
    {
        if (result is null)
            return new OpenNelActionResult(false, "操作失败", "null_result");

        string type = GetProperty(result, "type") ?? string.Empty;
        if (type.Equals(successType, StringComparison.OrdinalIgnoreCase))
            return new OpenNelActionResult(true, successMessage, type, Phone: GetProperty(result, "phone"));

        return new OpenNelActionResult(false, GetProperty(result, "message") ?? "操作失败",
            string.IsNullOrWhiteSpace(type) ? "unknown_error" : type,
            VerifyUrl: GetProperty(result, "verify_url"),
            CaptchaUrl: GetProperty(result, "captchaUrl"),
            SessionId: GetProperty(result, "sessionId"),
            EntityId: GetProperty(result, "entityId"),
            Phone: GetProperty(result, "phone"));
    }

    private static bool TryGetSucceededEntityId(object result, out string entityId)
    {
        entityId = string.Empty;
        if (result is not IEnumerable items)
            return false;

        foreach (object? item in items.Cast<object?>())
        {
            if (item is null)
                continue;

            if (!string.Equals(GetProperty(item, "type"), "Success_login", StringComparison.OrdinalIgnoreCase))
                continue;

            entityId = GetProperty(item, "entityId") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(entityId))
                return true;
        }

        return false;
    }

    private static string? GetProperty(object source, string propertyName)
    {
        return source.GetType().GetProperty(propertyName)?.GetValue(source)?.ToString();
    }

    private static OpenNelLoginKind InferLoginKind(string? accountType)
    {
        return (accountType ?? string.Empty).ToLowerInvariant() switch
        {
            "password" or "4399" => OpenNelLoginKind.Login4399Password,
            "cookie" => OpenNelLoginKind.NeteaseCookie,
            "email" => OpenNelLoginKind.NeteaseEmail,
            "phone" => OpenNelLoginKind.NeteasePhone,
            _ => OpenNelLoginKind.Unknown
        };
    }

    private static OpenNelProvider InferProvider(OpenNelLoginKind loginKind)
    {
        return loginKind == OpenNelLoginKind.Login4399Password
            ? OpenNelProvider.Game4399
            : OpenNelProvider.Netease;
    }
}
