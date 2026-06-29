extern alias PclPortable;

using System.Windows;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.Utils.Validate;
using PCL.Online.OpenNel;
using CommunityLoginCatalog = PclPortable::PCL.Core.App.Login.CommunityLoginCatalog;
using CommunityLoginMethod = PclPortable::PCL.Core.App.Login.CommunityLoginMethod;
using CommunityLoginProvider = PclPortable::PCL.Core.App.Login.CommunityLoginProvider;

namespace PCL;

public partial class PageLoginNetEase
{
    private DispatcherTimer? _codeTimer;
    private int _codeCountdown;

    public Action? RequestClose { get; set; }
    public Action<ModLaunch.McLoginType>? RequestSwitchPlatform { get; set; }

    public PageLoginNetEase()
    {
        InitializeComponent();

        BtnPlatformNetease.Click += (_, _) => SetPlatformButtons(ModLaunch.McLoginType.NetEase);
        BtnPlatform4399.Click += (_, _) => RequestSwitchPlatform?.Invoke(ModLaunch.McLoginType._4399);
        BtnMethodEmail.Click += (_, _) => ShowMethod(CommunityLoginMethod.NetEaseEmailPassword);
        BtnMethodSms.Click += (_, _) => ShowMethod(CommunityLoginMethod.NetEaseSmsCode);
        BtnMethodCookie.Click += (_, _) => ShowMethod(CommunityLoginMethod.NetEaseCookie);
        BtnEmailLogin.Click += BtnEmailLogin_Click;
        BtnCookieLogin.Click += BtnCookieLogin_Click;
        BtnSmsLogin.Click += BtnSmsLogin_Click;
        BtnSendCode.Click += BtnSendCode_Click;
        Loaded += (_, _) => ResetPage();
    }

    private void ResetPage()
    {
        StopCodeCountdown();
        TextEmail.Text = null;
        TextEmailPass.Password = null;
        TextCookieName.Text = null;
        TextCookie.Text = null;
        TextSmsPhone.Text = null;
        TextSmsCode.Text = null;
        SetPlatformButtons(ModLaunch.McLoginType.NetEase);
        ShowMethod(CommunityLoginMethod.NetEaseEmailPassword);
    }

    private void ShowMethod(CommunityLoginMethod method)
    {
        var definitions = CommunityLoginCatalog.GetMethods(CommunityLoginProvider.NetEase);
        var definition = definitions.First(x => x.Method == method);
        LabMethodDescription.Text = definition.Description;

        BtnMethodEmail.ColorType = method == CommunityLoginMethod.NetEaseEmailPassword
            ? MyButton.ColorState.Highlight
            : MyButton.ColorState.Normal;
        BtnMethodSms.ColorType = method == CommunityLoginMethod.NetEaseSmsCode
            ? MyButton.ColorState.Highlight
            : MyButton.ColorState.Normal;
        BtnMethodCookie.ColorType = method == CommunityLoginMethod.NetEaseCookie
            ? MyButton.ColorState.Highlight
            : MyButton.ColorState.Normal;

        CardEmailLogin.Visibility = method == CommunityLoginMethod.NetEaseEmailPassword
            ? Visibility.Visible
            : Visibility.Collapsed;
        CardSmsLogin.Visibility = method == CommunityLoginMethod.NetEaseSmsCode
            ? Visibility.Visible
            : Visibility.Collapsed;
        CardCookieLogin.Visibility = method == CommunityLoginMethod.NetEaseCookie
            ? Visibility.Visible
            : Visibility.Collapsed;
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

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static bool IsValidPhone(string phone) =>
        new RegexValidator("^1[3-9]\\d{9}$").Validate(phone).IsValid;

    private void BtnEmailLogin_Click(object sender, EventArgs e)
    {
        var email = Normalize(TextEmail.Text);
        var password = TextEmailPass.Password;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModMain.Hint("请输入网易邮箱和密码", ModMain.HintType.Critical);
            return;
        }

        DoLogin(new ModLaunch.McLoginNetEase
        {
            UserName = email,
            Password = password,
            DisplayName = email,
            CommunityLoginKind = OpenNelLoginKind.NeteaseEmail
        }, BtnEmailLogin);
    }

    private void BtnCookieLogin_Click(object sender, EventArgs e)
    {
        var displayName = Normalize(TextCookieName.Text);
        var cookie = Normalize(TextCookie.Text);
        if (string.IsNullOrEmpty(cookie))
        {
            ModMain.Hint("请先粘贴 Netease Cookie", ModMain.HintType.Critical);
            return;
        }

        if (string.IsNullOrEmpty(displayName))
            displayName = "Netease Cookie";

        DoLogin(new ModLaunch.McLoginNetEase
        {
            UserName = displayName,
            DisplayName = displayName,
            AccessToken = cookie,
            CommunityLoginKind = OpenNelLoginKind.NeteaseCookie
        }, BtnCookieLogin);
    }

    private void BtnSmsLogin_Click(object sender, EventArgs e)
    {
        var phone = Normalize(TextSmsPhone.Text);
        var code = Normalize(TextSmsCode.Text);

        if (string.IsNullOrEmpty(phone))
        {
            ModMain.Hint("请先输入手机号", ModMain.HintType.Critical);
            return;
        }

        if (!IsValidPhone(phone))
        {
            ModMain.Hint("手机号格式不正确", ModMain.HintType.Critical);
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            ModMain.Hint("请输入短信验证码", ModMain.HintType.Critical);
            return;
        }

        DoLogin(new ModLaunch.McLoginNetEase
        {
            UserName = phone,
            VerifyCode = code,
            DisplayName = phone,
            CommunityLoginKind = OpenNelLoginKind.NeteasePhone
        }, BtnSmsLogin);
    }

    private void BtnSendCode_Click(object sender, EventArgs e)
    {
        var phone = Normalize(TextSmsPhone.Text);
        if (string.IsNullOrEmpty(phone))
        {
            ModMain.Hint("请先输入手机号", ModMain.HintType.Critical);
            return;
        }

        if (!IsValidPhone(phone))
        {
            ModMain.Hint("手机号格式不正确", ModMain.HintType.Critical);
            return;
        }

        BtnSendCode.IsEnabled = false;

        ModBase.RunInNewThread(() =>
        {
            var result = OpenNelAccountService.SendNeteaseSmsCode(phone);
            ModBase.RunInUi(() =>
            {
                if (!result.Success)
                {
                    BtnSendCode.IsEnabled = true;
                    ModMain.Hint($"发送验证码失败：{result.Message}", ModMain.HintType.Critical);
                    return;
                }

                _codeCountdown = 60;
                ModMain.Hint("验证码已发送，请查收短信", ModMain.HintType.Finish);
                StartCodeCountdown();
            });
        }, "Netease SMS Send");
    }

    private void StartCodeCountdown()
    {
        StopCodeCountdown();
        BtnSendCode.Text = $"{_codeCountdown}s";
        _codeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _codeTimer.Tick += (_, _) =>
        {
            _codeCountdown--;
            if (_codeCountdown <= 0)
            {
                StopCodeCountdown();
                return;
            }

            BtnSendCode.Text = $"{_codeCountdown}s";
        };
        _codeTimer.Start();
    }

    private void StopCodeCountdown()
    {
        if (_codeTimer is not null)
        {
            _codeTimer.Stop();
            _codeTimer = null;
        }

        BtnSendCode.IsEnabled = true;
        BtnSendCode.Text = "发送验证码";
    }

    private void DoLogin(ModLaunch.McLoginNetEase loginData, MyButton actionButton)
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

        var originalText = actionButton.Text;
        PanRoot.IsEnabled = false;

        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            try
            {
                ModProfile.isCreatingProfile = true;
                ModLaunch.mcLoginNetEaseLoader.Start(loginData, true);
                while (ModLaunch.mcLoginNetEaseLoader.State == ModBase.LoadState.Loading)
                {
                    actionButton.Text = Lang.Number(ModLaunch.mcLoginNetEaseLoader.Progress, "P0");
                    await Task.Delay(50);
                }

                switch (ModLaunch.mcLoginNetEaseLoader.State)
                {
                    case ModBase.LoadState.Finished:
                        ResetPage();
                        RequestClose?.Invoke();
                        ModMain.frmLaunchLeft.RefreshPage(true);
                        break;
                    case ModBase.LoadState.Aborted:
                        ModMain.Hint("Netease 登录已取消");
                        break;
                    default:
                    {
                        if (ModLaunch.mcLoginNetEaseLoader.Error is null)
                            throw new Exception("Netease 登录：未知错误");
                        throw new Exception(ModLaunch.mcLoginNetEaseLoader.Error.Message,
                            ModLaunch.mcLoginNetEaseLoader.Error);
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
                    ModBase.Log(ex, "Netease 登录失败", ModBase.LogLevel.Msgbox);
                }
            }
            finally
            {
                ModProfile.isCreatingProfile = false;
                PanRoot.IsEnabled = true;
                actionButton.Text = originalText;
            }
        }));
    }
}
