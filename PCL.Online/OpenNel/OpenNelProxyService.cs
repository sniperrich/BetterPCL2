using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Entities.Web.NetGame;
using OpenNEL.Handlers.PC;
using OpenNEL.Handlers.PC.Game.NetGame;

namespace PCL.Online.OpenNel;

public static class OpenNelProxyService
{
    public static OpenNelServerListResult ListServers(int offset = 0, int pageSize = 20)
    {
        OpenNelRuntime.EnsureInitialized();
        ListServersResult result = new ListServers().Execute(offset, pageSize);
        return new OpenNelServerListResult(
            Success: result.Success,
            Message: result.NotLogin ? "未登录游戏账号" : result.Message ?? string.Empty,
            Items: result.Items.Select(MapServer).ToArray(),
            HasMore: result.HasMore,
            NotLoggedIn: result.NotLogin);
    }

    public static OpenNelServerListResult SearchServers(string keyword)
    {
        OpenNelRuntime.EnsureInitialized();
        ListServersResult result = new SearchServers().Execute(keyword);
        return new OpenNelServerListResult(
            Success: result.Success,
            Message: result.NotLogin ? "未登录游戏账号" : result.Message ?? string.Empty,
            Items: result.Items.Select(MapServer).ToArray(),
            HasMore: result.HasMore,
            NotLoggedIn: result.NotLogin);
    }

    public static OpenNelRoleListResult GetRoles(string serverId)
    {
        OpenNelRuntime.EnsureInitialized();
        ServerRolesResult result = new GetRoleNamed().Execute(serverId);
        return new OpenNelRoleListResult(
            Success: result.Success,
            Message: result.NotLogin ? "未登录游戏账号" : result.Message ?? string.Empty,
            ServerId: result.ServerId,
            Items: result.Items.Select(MapRole).ToArray(),
            NotLoggedIn: result.NotLogin);
    }

    public static OpenNelRoleListResult CreateRole(string serverId, string roleName)
    {
        OpenNelRuntime.EnsureInitialized();
        ServerRolesResult result = new CreateRoleNamed().Execute(serverId, roleName);
        return new OpenNelRoleListResult(
            Success: result.Success,
            Message: result.NotLogin ? "未登录游戏账号" : result.Message ?? string.Empty,
            ServerId: result.ServerId,
            Items: result.Items.Select(MapRole).ToArray(),
            NotLoggedIn: result.NotLogin);
    }

    /// <summary>
    /// 启动本地 MITM 代理 — 使用 JoinGame (纯代理模式)，不启动白端游戏客户端。
    /// 返回 127.0.0.1:端口 供 Minecraft Java 直接连接。
    /// </summary>
    public static OpenNelProxyLaunchResult LaunchGame(string serverId, string serverName, string roleName,
        string accountId = "", bool isRental = false)
    {
        OpenNelRuntime.EnsureInitialized();
        var result = new JoinGame().Execute(accountId, serverId, serverName, roleName)
            .GetAwaiter().GetResult();

        if (!result.Success)
        {
            return new OpenNelProxyLaunchResult(
                Success: false,
                Message: result.NotLogin ? "未登录游戏账号" : result.Message ?? "代理启动失败",
                Identifier: string.Empty,
                LocalAddress: string.Empty);
        }

        return new OpenNelProxyLaunchResult(
            Success: true,
            Message: $"MITM 代理已启动: {result.Ip}:{result.Port}",
            Identifier: Guid.NewGuid().ToString(), // 占位 — 实际标识从 QuerySessions 获取
            LocalAddress: $"{result.Ip}:{result.Port}");
    }

    public static IReadOnlyList<OpenNelProxySession> QuerySessions()
    {
        OpenNelRuntime.EnsureInitialized();
        object result = new QueryGameSession().Execute();
        if (GetProperty(result, "items") is not IEnumerable items)
            return Array.Empty<OpenNelProxySession>();

        List<OpenNelProxySession> sessions = new();
        foreach (object? item in items.Cast<object?>())
        {
            if (item is null)
                continue;

            // 优先使用 Guid 字段 (proxy 的实际 GUID)，fallback 到 Id
            string guid = GetProperty(item, "Guid")?.ToString() ?? string.Empty;
            string id = GetProperty(item, "Id")?.ToString() ?? string.Empty;
            string sessionType = GetProperty(item, "Type")?.ToString() ?? string.Empty;

            // 对 proxy 类型，使用 "proxy-{guid}" 作为标识以确保 shutdown 能匹配
            string sessionId = sessionType == "Proxy" && !string.IsNullOrWhiteSpace(guid)
                ? $"proxy-{guid}"
                : id;

            sessions.Add(new OpenNelProxySession(
                Id: sessionId,
                ServerName: GetProperty(item, "ServerName")?.ToString() ?? string.Empty,
                CharacterName: GetProperty(item, "CharacterName")?.ToString() ?? string.Empty,
                ServerVersion: GetProperty(item, "ServerVersion")?.ToString() ?? string.Empty,
                StatusText: GetProperty(item, "StatusText")?.ToString() ?? string.Empty,
                ProgressValue: TryGetInt(item, "ProgressValue"),
                SessionType: sessionType,
                LocalAddress: GetProperty(item, "LocalAddress")?.ToString() ?? string.Empty));
        }

        return sessions;
    }

    public static OpenNelShutdownResult ShutdownSessions(IEnumerable<string> identifiers)
    {
        OpenNelRuntime.EnsureInitialized();
        object[] result = new ShutdownGame().Execute(identifiers);
        IEnumerable<string> acked = Array.Empty<string>();

        foreach (object item in result)
        {
            if (!string.Equals(GetProperty(item, "type")?.ToString(), "shutdown_ack", StringComparison.OrdinalIgnoreCase))
                continue;

            if (GetProperty(item, "identifiers") is IEnumerable values)
                acked = values.Cast<object?>().Select(value => value?.ToString() ?? string.Empty)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
            break;
        }

        return new OpenNelShutdownResult(true, "已停止所选会话", acked.ToArray());
    }

    private static OpenNelServerItem MapServer(ServerItem item)
    {
        return new OpenNelServerItem(
            EntityId: item.EntityId,
            Name: item.Name,
            OnlineCount: item.OnlineCount,
            ImageUrl: item.ImageUrl);
    }

    private static OpenNelRoleItem MapRole(RoleItem item)
    {
        return new OpenNelRoleItem(
            Id: item.Id,
            Name: item.Name);
    }

    private static object? GetProperty(object source, string propertyName)
    {
        return source.GetType().GetProperty(propertyName)?.GetValue(source);
    }

    private static int TryGetInt(object source, string propertyName)
    {
        object? value = GetProperty(source, propertyName);
        return value switch
        {
            int number => number,
            _ when int.TryParse(value?.ToString(), out int parsed) => parsed,
            _ => 0
        };
    }
}
