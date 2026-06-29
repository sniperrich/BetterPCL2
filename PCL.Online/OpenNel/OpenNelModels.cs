using System.Collections.Generic;

namespace PCL.Online.OpenNel;

public enum OpenNelProvider
{
    Unknown = 0,
    Game4399 = 1,
    Netease = 2
}

public enum OpenNelLoginKind
{
    Unknown = 0,
    Login4399Password = 1,
    NeteaseCookie = 2,
    NeteasePhone = 3,
    NeteaseEmail = 4
}

public sealed record OpenNelAccountResult(
    OpenNelProvider Provider,
    OpenNelLoginKind LoginKind,
    string EntityId,
    string DisplayName,
    string AccessToken,
    string LoginChannel,
    string PersistedDetailsJson);

public sealed record OpenNelPortableProfile(
    string Uuid,
    string Username,
    string AccessToken,
    OpenNelLoginKind LoginKind,
    string DetailsJson);

public sealed record OpenNelAccountLoginResult(
    bool Success,
    string Message,
    OpenNelAccountResult? Account = null,
    string Code = "",
    string? VerifyUrl = null,
    string? CaptchaUrl = null,
    string? SessionId = null,
    string? AccountName = null,
    string? Password = null,
    string? EntityId = null,
    string? Phone = null);

public sealed record OpenNelActionResult(
    bool Success,
    string Message,
    string Code = "",
    string? VerifyUrl = null,
    string? CaptchaUrl = null,
    string? SessionId = null,
    string? EntityId = null,
    string? Phone = null);

public sealed record OpenNelServerItem(
    string EntityId,
    string Name,
    string OnlineCount,
    string ImageUrl);

public sealed record OpenNelServerListResult(
    bool Success,
    string Message,
    IReadOnlyList<OpenNelServerItem> Items,
    bool HasMore,
    bool NotLoggedIn);

public sealed record OpenNelRoleItem(
    string Id,
    string Name);

public sealed record OpenNelRoleListResult(
    bool Success,
    string Message,
    string ServerId,
    IReadOnlyList<OpenNelRoleItem> Items,
    bool NotLoggedIn);

public sealed record OpenNelProxyLaunchResult(
    bool Success,
    string Message,
    string Identifier,
    string LocalAddress = "");

public sealed record OpenNelProxySession(
    string Id,
    string ServerName,
    string CharacterName,
    string ServerVersion,
    string StatusText,
    int ProgressValue,
    string SessionType,
    string LocalAddress);

public sealed record OpenNelShutdownResult(
    bool Success,
    string Message,
    IReadOnlyList<string> Identifiers);
