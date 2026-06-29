# OpenNEL Community Login Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use $superpower-subagents (recommended) or $superpower-executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking via update_plan.

**Goal:** Align 4399 login, Netease login, and the Netease Java proxy workflow with the real implementations under `C:\Users\Administrator\Downloads\OPENNEL_DUMP\Decompiled`, while keeping the launcher UI usable in a constrained window.

**Architecture:** Add a thin `PCL.Online` bridge over `OpenNEL` so the real login and proxy handlers can be reused without duplicating their transport logic in `Plain Craft Launcher 2`. Extend profile persistence to retain provider-specific details and login subtypes, then replace the fake community-login flows in `ModLaunch` and the WPF pages with bridge-backed calls and a dedicated proxy page under the existing online section.

**Tech Stack:** .NET 10, WPF, MSTest, `PCL.Online`, external `OpenNEL` project references

---

### Task 1: Add a Bridge Contract and Lock Result Mapping with Tests

**Files:**
- Create: `PCL.Online/OpenNel/OpenNelModels.cs`
- Create: `PCL.Online/OpenNel/OpenNelProfileMapper.cs`
- Create: `PCL.Online.Test/OpenNel/OpenNelProfileMapperTests.cs`
- Modify: `PCL.Online/PCL.Online.csproj`

- [ ] **Step 1: Write the failing test**

```csharp
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
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PCL.Online.Test/PCL.Online.Test.csproj --filter OpenNelProfileMapperTests -v minimal`
Expected: FAIL because the mapper and bridge models do not exist yet.

- [ ] **Step 3: Write the minimal implementation**

```csharp
public enum OpenNelLoginKind
{
    Unknown,
    Login4399Password,
    NeteaseCookie,
    NeteasePhone,
    NeteaseEmail
}

public sealed record OpenNelPortableProfile(
    string Uuid,
    string Username,
    string AccessToken,
    OpenNelLoginKind LoginKind,
    string DetailsJson);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test PCL.Online.Test/PCL.Online.Test.csproj --filter OpenNelProfileMapperTests -v minimal`
Expected: PASS with `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add PCL.Online/OpenNel/OpenNelModels.cs PCL.Online/OpenNel/OpenNelProfileMapper.cs PCL.Online.Test/OpenNel/OpenNelProfileMapperTests.cs PCL.Online/PCL.Online.csproj
git commit -m "test: lock OpenNEL profile mapping"
```

### Task 2: Reuse the Real OpenNEL Login and Proxy Handlers

**Files:**
- Create: `PCL.Online/OpenNel/OpenNelRuntime.cs`
- Create: `PCL.Online/OpenNel/OpenNelAccountService.cs`
- Create: `PCL.Online/OpenNel/OpenNelProxyService.cs`
- Modify: `PCL.Online/PCL.Online.csproj`
- Test: `PCL.Online.Test/OpenNel/OpenNelProfileMapperTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test PCL.Online.Test/PCL.Online.Test.csproj --filter NormalizeProxySession_ShouldKeepLocalAddressAndType -v minimal`
Expected: FAIL because the proxy session model does not exist yet.

- [ ] **Step 3: Write minimal bridge implementation**

```csharp
public static class OpenNelRuntime
{
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        OpenNEL.Backend.Initialize();
    }
}
```

- [ ] **Step 4: Run targeted tests**

Run: `dotnet test PCL.Online.Test/PCL.Online.Test.csproj --filter OpenNel -v minimal`
Expected: PASS for bridge model tests.

- [ ] **Step 5: Commit**

```bash
git add PCL.Online/OpenNel/OpenNelRuntime.cs PCL.Online/OpenNel/OpenNelAccountService.cs PCL.Online/OpenNel/OpenNelProxyService.cs PCL.Online/PCL.Online.csproj PCL.Online.Test/OpenNel/OpenNelProfileMapperTests.cs
git commit -m "feat: add OpenNEL bridge services"
```

### Task 3: Replace Fake Community Login Storage and Activation in the Launcher

**Files:**
- Modify: `Plain Craft Launcher 2/Modules/Minecraft/ModProfile.cs`
- Modify: `Plain Craft Launcher 2/Modules/Minecraft/ModLaunch.cs`
- Modify: `Plain Craft Launcher 2/Pages/PageLaunch/PageLogin4399.xaml.cs`
- Modify: `Plain Craft Launcher 2/Pages/PageLaunch/PageLoginNetEase.xaml`
- Modify: `Plain Craft Launcher 2/Pages/PageLaunch/PageLoginNetEase.xaml.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Manual regression target:
// 1. Existing 4399 profile should still load from profiles.json.
// 2. New Netease email profile should persist subtype and details JSON.
// 3. Netease SMS send/verify should call different bridge methods.
```

- [ ] **Step 2: Run a focused build to verify current integration fails or is incomplete**

Run: `dotnet build "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" -c Release -clp:ErrorsOnly`
Expected: either compile failures after model changes or the current fake logic still present.

- [ ] **Step 3: Implement the launcher integration**

```csharp
var login = OpenNelAccountService.LoginNeteaseEmail(email, password);
var mapped = OpenNelProfileMapper.ToPortableProfile(login);
// Map to ModProfile.McProfile and save subtype/details fields.
```

- [ ] **Step 4: Rebuild the launcher**

Run: `dotnet build "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" -c Release -clp:ErrorsOnly`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add "Plain Craft Launcher 2/Modules/Minecraft/ModProfile.cs" "Plain Craft Launcher 2/Modules/Minecraft/ModLaunch.cs" "Plain Craft Launcher 2/Pages/PageLaunch/PageLogin4399.xaml.cs" "Plain Craft Launcher 2/Pages/PageLaunch/PageLoginNetEase.xaml" "Plain Craft Launcher 2/Pages/PageLaunch/PageLoginNetEase.xaml.cs"
git commit -m "feat: align community login with OpenNEL"
```

### Task 4: Add a Dedicated Netease Java Proxy Page and Wire It Into Online Navigation

**Files:**
- Create: `Plain Craft Launcher 2/Pages/PageOnline/PageOnlineNeteaseProxy.xaml`
- Create: `Plain Craft Launcher 2/Pages/PageOnline/PageOnlineNeteaseProxy.xaml.cs`
- Modify: `Plain Craft Launcher 2/Pages/PageOnline/PageOnlineLeft.xaml`
- Modify: `Plain Craft Launcher 2/Pages/PageOnline/PageOnlineLeft.xaml.cs`
- Modify: `Plain Craft Launcher 2/FormMain.xaml.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Manual regression target:
// 1. Online left nav exposes a visible Netease Java proxy entry.
// 2. Opening the page shows server list, search, role actions, launch actions, and session list without overflowing.
// 3. Shutdown action removes proxy or launcher sessions.
```

- [ ] **Step 2: Run a build before the page exists**

Run: `dotnet build "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" -c Release -clp:ErrorsOnly`
Expected: current build passes, but the feature is still missing.

- [ ] **Step 3: Implement the page and navigation**

```csharp
PageChange(new FormMain.PageStackData
{
    page = FormMain.PageType.Online,
    additional = (int)FormMain.PageSubType.OnlineNeteaseProxy
});
```

- [ ] **Step 4: Build the full solution**

Run: `dotnet build "Plain Craft Launcher 2.slnx" -c Release -clp:ErrorsOnly`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add "Plain Craft Launcher 2/Pages/PageOnline/PageOnlineNeteaseProxy.xaml" "Plain Craft Launcher 2/Pages/PageOnline/PageOnlineNeteaseProxy.xaml.cs" "Plain Craft Launcher 2/Pages/PageOnline/PageOnlineLeft.xaml" "Plain Craft Launcher 2/Pages/PageOnline/PageOnlineLeft.xaml.cs" "Plain Craft Launcher 2/FormMain.xaml.cs"
git commit -m "feat: add Netease Java proxy page"
```

### Verification

- Run: `dotnet test PCL.Online.Test/PCL.Online.Test.csproj -v minimal`
- Run: `dotnet build "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" -c Release -clp:ErrorsOnly`
- Run: `dotnet build "Plain Craft Launcher 2.slnx" -c Release -clp:ErrorsOnly`
- Manual smoke test:
  - Open community login overlay and verify first-level choices are `4399` and `Netease`.
  - Verify Netease second-level choices are cookie, phone + SMS, and email + password.
  - Verify 4399 keeps its own password form and does not close the modal unexpectedly.
  - Verify the dedicated Netease Java proxy page can list servers, fetch/create role names, start a proxy launch, and show active sessions.

### Next skill

`$superpower-executing-plans`
