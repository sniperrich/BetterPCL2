using System.Windows;
using System.Windows.Controls;
using PCL.Core.App;
using PCL.Core.UI.Controls;

namespace PCL;

public partial class MyMsgCommunityLogin
{
    private readonly int _uuid = ModBase.GetUuid();

    public MyMsgCommunityLogin(string title, string subtitle, FrameworkElement content)
    {
        InitializeComponent();
        LabTitle.Text = title;
        LabSubtitle.Text = subtitle;
        content.Margin = new Thickness(0);
        PanContent.Content = content;
        BtnClose.Click += (_, _) => Close();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var maxWidth = Math.Min(560, Math.Max(320, ModMain.frmMain.ActualWidth - 120));
        var maxHeight = Math.Max(320, ModMain.frmMain.ActualHeight - 120);
        PanBorder.Width = maxWidth;
        PanBorder.MaxHeight = maxHeight;
        Opacity = 0;
        ModAnimation.AniStart(
            ModAnimation.AaColor(ModMain.frmMain.PanMsgBackground, BlurBorder.BackgroundProperty,
                new ModBase.MyColor(90d, 0d, 0d, 0d) - ModMain.frmMain.PanMsgBackground.Background, 180),
            "CommunityLoginBackground");
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaOpacity(this, 1d, 100, 40),
                ModAnimation.AaDouble(i => TransformPos.Y += (double)i,
                    -TransformPos.Y, 220, 20, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
                ModAnimation.AaDouble(i => TransformRotate.Angle += (double)i,
                    -TransformRotate.Angle, 220, 20, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
            }, "CommunityLoginDialog " + _uuid);
    }

    public void Close()
    {
        ModProfile.isCreatingProfile = false;
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaCode(() =>
                {
                    if (ModMain.frmMain.PanMsg.Children.Count <= 1 && !ModMain.WaitingMyMsgBox.Any())
                    {
                        ModAnimation.AniStart(
                            ModAnimation.AaColor(ModMain.frmMain.PanMsgBackground, BlurBorder.BackgroundProperty,
                                new ModBase.MyColor(0d, 0d, 0d, 0d) - ModMain.frmMain.PanMsgBackground.Background, 180),
                            "CommunityLoginBackground");
                    }
                }, 20),
                ModAnimation.AaOpacity(this, -Opacity, 80, 0),
                ModAnimation.AaDouble(i => TransformPos.Y += (double)i, 18d - TransformPos.Y,
                    140, 0, new ModAnimation.AniEaseOutFluent()),
                ModAnimation.AaDouble(i => TransformRotate.Angle += (double)i,
                    4d - TransformRotate.Angle, 140, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                ModAnimation.AaCode(() =>
                {
                    if (Parent is Grid grid)
                        grid.Children.Remove(this);
                    if (ModMain.frmMain.PanMsg.Children.Count == 0 && !ModMain.WaitingMyMsgBox.Any())
                        ModMain.frmMain.PanMsgBackground.Visibility = Visibility.Collapsed;
                }, after: true)
            }, "CommunityLoginDialog " + _uuid);
    }
}
