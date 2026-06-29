using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.App.Login;

namespace PCL.Core.Portable.Test;

[TestClass]
public class CommunityLoginCatalogTests
{
    [TestMethod]
    public void NetEaseProvider_ShouldExposeCookieAndSmsMethods()
    {
        var methods = CommunityLoginCatalog.GetMethods(CommunityLoginProvider.NetEase);

        Assert.AreEqual(2, methods.Count);
        Assert.AreEqual(CommunityLoginMethod.NetEaseCookie, methods[0].Method);
        Assert.AreEqual(CommunityLoginMethod.NetEaseSmsCode, methods[1].Method);
        Assert.IsTrue(CommunityLoginCatalog.IsTokenOnlyMethod(CommunityLoginMethod.NetEaseCookie));
        Assert.IsFalse(CommunityLoginCatalog.IsTokenOnlyMethod(CommunityLoginMethod.NetEaseSmsCode));
    }

    [TestMethod]
    public void _4399Provider_ShouldExposePasswordMethodOnly()
    {
        var methods = CommunityLoginCatalog.GetMethods(CommunityLoginProvider.Game4399);

        Assert.AreEqual(1, methods.Count);
        Assert.AreEqual(CommunityLoginMethod.Game4399Password, methods[0].Method);
        Assert.IsFalse(CommunityLoginCatalog.IsTokenOnlyMethod(CommunityLoginMethod.Game4399Password));
    }
}
