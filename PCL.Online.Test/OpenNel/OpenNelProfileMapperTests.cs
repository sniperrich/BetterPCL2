using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Online.OpenNel;

namespace PCL.Online.Test.OpenNel;

[TestClass]
public sealed class OpenNelProfileMapperTests
{
    [TestMethod]
    public void MapNeteaseEmailLogin_ShouldPersistSubtypeAndDetails()
    {
        var result = new OpenNelAccountResult(
            Provider: OpenNelProvider.Netease,
            LoginKind: OpenNelLoginKind.NeteaseEmail,
            EntityId: "12345",
            DisplayName: "mail@example.com",
            AccessToken: "token-a",
            LoginChannel: "netease",
            PersistedDetailsJson: "{\"kind\":\"email\",\"email\":\"mail@example.com\",\"password\":\"p\"}");

        var mapped = OpenNelProfileMapper.ToPortableProfile(result);

        Assert.AreEqual("12345", mapped.Uuid);
        Assert.AreEqual("mail@example.com", mapped.Username);
        Assert.AreEqual(OpenNelLoginKind.NeteaseEmail, mapped.LoginKind);
        Assert.AreEqual("{\"kind\":\"email\",\"email\":\"mail@example.com\",\"password\":\"p\"}", mapped.DetailsJson);
    }

    [TestMethod]
    public void NormalizeProxySession_ShouldKeepLocalAddressAndType()
    {
        var session = new OpenNelProxySession(
            Id: "proxy-abc",
            ServerName: "测试服",
            CharacterName: "RoleA",
            ServerVersion: "1.20.1",
            StatusText: "Running",
            ProgressValue: 0,
            SessionType: "Proxy",
            LocalAddress: "127.0.0.1:25565");

        Assert.AreEqual("Proxy", session.SessionType);
        Assert.AreEqual("127.0.0.1:25565", session.LocalAddress);
    }
}
