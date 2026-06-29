namespace PCL.Core.App.Login;

public enum CommunityLoginProvider
{
    NetEase,
    Game4399
}

public enum CommunityLoginMethod
{
    NetEaseCookie,
    NetEaseSmsCode,
    NetEaseEmailPassword,
    Game4399Password
}

public readonly record struct CommunityLoginMethodDefinition(
    CommunityLoginMethod Method,
    string Title,
    string Description);

public static class CommunityLoginCatalog
{
    private static readonly IReadOnlyList<CommunityLoginMethodDefinition> _NetEaseMethods =
    [
        new(CommunityLoginMethod.NetEaseCookie, "Cookie 登录", "适合已经抓到网易会话 Cookie 的情况。"),
        new(CommunityLoginMethod.NetEaseSmsCode, "手机号 + 验证码", "先发短信，再用验证码继续登录。"),
        new(CommunityLoginMethod.NetEaseEmailPassword, "邮箱 + 密码", "直接使用网易邮箱和密码登录。")
    ];

    private static readonly IReadOnlyList<CommunityLoginMethodDefinition> _4399Methods =
    [
        new(CommunityLoginMethod.Game4399Password, "账号密码登录", "输入 4399 账号或手机号以及密码。")
    ];

    public static IReadOnlyList<CommunityLoginMethodDefinition> GetMethods(CommunityLoginProvider provider) =>
        provider switch
        {
            CommunityLoginProvider.NetEase => _NetEaseMethods,
            CommunityLoginProvider.Game4399 => _4399Methods,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };

    public static bool IsTokenOnlyMethod(CommunityLoginMethod method) =>
        method == CommunityLoginMethod.NetEaseCookie;
}
