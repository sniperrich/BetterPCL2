extern alias PclPortable;

using System.Windows;
using PCL.Core.App.Localization;
using PCL.Online.OpenNel;
using CommunityLoginCatalog = PclPortable::PCL.Core.App.Login.CommunityLoginCatalog;
using CommunityLoginProvider = PclPortable::PCL.Core.App.Login.CommunityLoginProvider;

namespace PCL;

public partial class PageLogin4399
{
    public Action? RequestClose { get; set; }
    public Action<ModLaunch.McLoginType>? RequestSwitchPlatform { get; set; }

    public PageLogin4399()
    {
        InitializeComponent();

        BtnPlatformNetease.Click += (_, _) => RequestSwitchPlatform?.Invoke(ModLaunch.McLoginType.NetEase);
        BtnPlatform4399.Click += (_, _) => SetPlatformButtons(ModLaunch.McLoginType._4399);
        BtnLogin.Click += BtnLogin_Click;
        Loaded += (_, _) => ResetPage();
    }

    private void ResetPage()
    {
        LoadMethodDescriptions();
        TextName.Text = null;
        TextPass.Password = null;
        SetPlatformButtons(ModLaunch.McLoginType._4399);
    }

    private void LoadMethodDescriptions()
    {
        var method = CommunityLoginCatalog.GetMethods(CommunityLoginProvider.Game4399)[0];
        LabPasswordTitle.Text = method.Title;
        LabPasswordDesc.Text = method.Description;
    }

    private void SetPlatformButtons(ModLaunch.McLoginType type)
    {
        BtnPlatformNetease.ColorType = type == ModLaunch.McLoginType.NetEase
            ? MyButton.ColorState.Highlight
            : MyButton.ColorState.Normal;
        BtnPlatform4399.ColorType = type == ModLaunch.McLoginType._4399
            ? MyButton.ColorState.Highlight
            : MyButton.ColorState.Normal;
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        var username = TextName.Text?.Trim();
        var password = TextPass.Password;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModMain.Hint("请输入 4399 账号和密码", ModMain.HintType.Critical);
            return;
        }

        DoLogin(username, password);
    }

    private void DoLogin(string username, string password)
    {
        if (!ModProfile.profileList.Any(x => x.Type == ModLaunch.McLoginType.Ms))
        {
            var msWarnResult = ModMain.MyMsgBox(
                Lang.Text("Launch.Account.Unverified.Warning.Message"),
                Lang.Text("Launch.Account.Unverified.Warning.Title"),
                Lang.Text("Common.Action.Cancel"), Lang.Text("Launch.Account.Unverified.Warning.OpenStore"),
                Lang.Text("Launch.Account.Unverified.Warning.Continue"),
                isWarn: true, forceWait: true);
            if (msWarnResult == 1 || msWarnResult == 2)
            {
                if (msWarnResult == 2)
                    ModBase.OpenWebsite("https://www.minecraft.net");
                return;
            }
        }

        var originalText = BtnLogin.Text;
        PanRoot.IsEnabled = false;

        var loginData = new ModLaunch.McLogin4399
        {
            UserName = username,
            Password = password,
            DisplayName = username,
            CommunityLoginKind = OpenNelLoginKind.Login4399Password
        };

        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            try
            {
                ModProfile.isCreatingProfile = true;
                ModLaunch.mcLogin4399Loader.Start(loginData, true);
                while (ModLaunch.mcLogin4399Loader.State == ModBase.LoadState.Loading)
                {
                    BtnLogin.Text = Lang.Number(ModLaunch.mcLogin4399Loader.Progress, "P0");
                    await Task.Delay(50);
                }

                switch (ModLaunch.mcLogin4399Loader.State)
                {
                    case ModBase.LoadState.Finished:
                        ResetPage();
                        RequestClose?.Invoke();
                        ModMain.frmLaunchLeft.RefreshPage(true);
                        break;
                    case ModBase.LoadState.Aborted:
                        ModMain.Hint("4399 登录已取消");
                        break;
                    default:
                    {
                        if (ModLaunch.mcLogin4399Loader.Error is null)
                            throw new Exception("4399 登录：未知错误");
                        throw new Exception(ModLaunch.mcLogin4399Loader.Error.Message,
                            ModLaunch.mcLogin4399Loader.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "$$")
                {
                }
                else if (ex.Message.StartsWith("$"))
                {
                    ModMain.Hint(ex.Message.TrimStart('$'), ModMain.HintType.Critical);
                }
                else
                {
                    ModBase.Log(ex, "4399 登录失败", ModBase.LogLevel.Msgbox);
                }
            }
            finally
            {
                ModProfile.isCreatingProfile = false;
                PanRoot.IsEnabled = true;
                BtnLogin.Text = originalText;
            }
        }));
    }
}
