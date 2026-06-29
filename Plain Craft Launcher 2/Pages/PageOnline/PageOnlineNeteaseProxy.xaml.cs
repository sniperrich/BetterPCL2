using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCL.Online.OpenNel;

namespace PCL;

public partial class PageOnlineNeteaseProxy : MyPageRight, IRefreshable
{
    private const int PageSize = 20;

    private bool _hasLoaded;
    private bool _isLoadingServers;
    private bool _isLoadingRoles;
    private bool _isLoadingSessions;
    private int _pageIndex;
    private List<OpenNelServerItem> _currentServers = [];
    private OpenNelServerItem? _selectedServer;

    public PageOnlineNeteaseProxy()
    {
        InitializeComponent();

        PanSearchBox.Search += (_, _) => StartNewSearch();
        PanSearchBox.KeyDown += SearchKeyDown;
        BtnServerPrev.Click += (_, _) => ChangePage(_pageIndex - 1);
        BtnServerNext.Click += (_, _) => ChangePage(_pageIndex + 1);
        BtnRefreshRoles.Click += async (_, _) => await RefreshRolesAsync();
        BtnCreateRole.Click += async (_, _) => await CreateRoleAsync();
        BtnLaunchProxy.Click += async (_, _) => await LaunchProxyAsync();
        PageEnter += () =>
        {
            if (!_hasLoaded)
                _ = RefreshAsync();
        };
    }

    public void Refresh()
    {
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _hasLoaded = true;
        UpdateAccountState();
        await LoadServersAsync();
        await RefreshSessionsAsync();
        if (_selectedServer is not null)
            await LoadRolesAsync(_selectedServer.EntityId);
    }

    private void UpdateAccountState()
    {
        var hasCommunityProfile = ModProfile.profileList.Any(profile =>
            profile.Type is ModLaunch.McLoginType._4399 or ModLaunch.McLoginType.NetEase);
        LabAccountState.Text = hasCommunityProfile
            ? "已检测到社区账号档案。如果列表返回未登录，请先重新完成一次 4399 或 Netease 登录。"
            : "还没有社区账号档案。请先在启动页登录 4399 或 Netease。";
    }

    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            StartNewSearch();
    }

    private void StartNewSearch()
    {
        _pageIndex = 0;
        _ = LoadServersAsync();
    }

    private void ChangePage(int nextPage)
    {
        if (nextPage < 0 || _isLoadingServers || !string.IsNullOrWhiteSpace(PanSearchBox.Text))
            return;

        _pageIndex = nextPage;
        _ = LoadServersAsync();
    }

    private async Task LoadServersAsync()
    {
        if (_isLoadingServers)
            return;

        _isLoadingServers = true;
        LoadServers.Visibility = Visibility.Visible;
        LabServerStatus.Text = "";
        PanServers.Children.Clear();
        PanServerPager.Visibility = Visibility.Collapsed;

        try
        {
            var keyword = PanSearchBox.Text?.Trim() ?? "";
            OpenNelServerListResult result = await Task.Run(() =>
                string.IsNullOrWhiteSpace(keyword)
                    ? OpenNelProxyService.ListServers(_pageIndex * PageSize, PageSize)
                    : OpenNelProxyService.SearchServers(keyword));

            _currentServers = result.Items.ToList();
            foreach (var server in _currentServers)
                PanServers.Children.Add(CreateServerCard(server));

            if (_currentServers.Count == 0)
                LabServerStatus.Text = string.IsNullOrWhiteSpace(result.Message) ? "没有找到可用服务器。" : result.Message;
            else
                LabServerStatus.Text = $"已获取 {_currentServers.Count} 个服务器。";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                PanServerPager.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanServerPager.Visibility = result.HasMore || _pageIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
                BtnServerPrev.IsEnabled = _pageIndex > 0;
                BtnServerPrev.Opacity = _pageIndex > 0 ? 1d : 0.25d;
                BtnServerNext.IsEnabled = result.HasMore;
                BtnServerNext.Opacity = result.HasMore ? 1d : 0.25d;
                LabServerPage.Text = $"第 {_pageIndex + 1} 页";
            }
        }
        finally
        {
            LoadServers.Visibility = Visibility.Collapsed;
            _isLoadingServers = false;
        }
    }

    private UIElement CreateServerCard(OpenNelServerItem server)
    {
        var card = new MyCard
        {
            Title = server.Name,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(16, 42, 16, 16)
        };

        panel.Children.Add(new TextBlock
        {
            Text = $"在线人数：{server.OnlineCount}",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = $"服务器 ID：{server.EntityId}",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7
        });

        var selectButton = new MyButton
        {
            Height = 32,
            Margin = new Thickness(0, 12, 0, 0),
            ColorType = _selectedServer?.EntityId == server.EntityId
                ? MyButton.ColorState.Highlight
                : MyButton.ColorState.Normal,
            Text = _selectedServer?.EntityId == server.EntityId ? "已选中" : "选择服务器"
        };
        selectButton.Click += async (_, _) =>
        {
            _selectedServer = server;
            LabSelectedServer.Text = $"当前服务器：{server.Name}";
            LabRoleStatus.Text = "正在读取角色列表…";
            await LoadRolesAsync(server.EntityId);
            RenderServerCards();
        };

        panel.Children.Add(selectButton);
        card.Children.Add(panel);
        return card;
    }

    private void RenderServerCards()
    {
        PanServers.Children.Clear();
        foreach (var server in _currentServers)
            PanServers.Children.Add(CreateServerCard(server));
    }

    private async Task LoadRolesAsync(string serverId)
    {
        if (_isLoadingRoles)
            return;

        _isLoadingRoles = true;
        LabRoleStatus.Text = "正在读取角色列表…";
        ComboRoles.Items.Clear();

        try
        {
            OpenNelRoleListResult result = await Task.Run(() => OpenNelProxyService.GetRoles(serverId));
            foreach (var role in result.Items)
                ComboRoles.Items.Add(new MyComboBoxItem { Content = role.Name, Tag = role.Id });

            if (ComboRoles.Items.Count > 0)
                ComboRoles.SelectedIndex = 0;

            LabRoleStatus.Text = result.Success
                ? (result.Items.Count > 0 ? $"已读取 {result.Items.Count} 个角色。" : "当前没有角色，可以先创建一个。")
                : (string.IsNullOrWhiteSpace(result.Message) ? "读取角色失败。" : result.Message);
        }
        finally
        {
            _isLoadingRoles = false;
        }
    }

    private async Task RefreshRolesAsync()
    {
        if (_selectedServer is null)
        {
            ModMain.Hint("请先选择一个服务器", ModMain.HintType.Critical);
            return;
        }

        await LoadRolesAsync(_selectedServer.EntityId);
    }

    private async Task CreateRoleAsync()
    {
        if (_selectedServer is null)
        {
            ModMain.Hint("请先选择一个服务器", ModMain.HintType.Critical);
            return;
        }

        var roleName = TextNewRole.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModMain.Hint("请输入新角色名", ModMain.HintType.Critical);
            return;
        }

        BtnCreateRole.IsEnabled = false;
        try
        {
            OpenNelRoleListResult result = await Task.Run(() =>
                OpenNelProxyService.CreateRole(_selectedServer.EntityId, roleName));
            if (!result.Success)
            {
                ModMain.Hint(string.IsNullOrWhiteSpace(result.Message) ? "创建角色失败" : result.Message,
                    ModMain.HintType.Critical);
                return;
            }

            TextNewRole.Text = "";
            ComboRoles.Items.Clear();
            foreach (var role in result.Items)
                ComboRoles.Items.Add(new MyComboBoxItem { Content = role.Name, Tag = role.Id });
            if (ComboRoles.Items.Count > 0)
            {
                var selectedIndex = 0;
                for (var i = 0; i < result.Items.Count; i++)
                    if (result.Items[i].Name == roleName)
                    {
                        selectedIndex = i;
                        break;
                    }

                ComboRoles.SelectedIndex = selectedIndex;
            }

            LabRoleStatus.Text = $"角色 {roleName} 已创建。";
            ModMain.Hint($"已创建角色 {roleName}", ModMain.HintType.Finish);
        }
        finally
        {
            BtnCreateRole.IsEnabled = true;
        }
    }

    private async Task LaunchProxyAsync()
    {
        if (_selectedServer is null)
        {
            ModMain.Hint("请先选择一个服务器", ModMain.HintType.Critical);
            return;
        }

        var roleName = (ComboRoles.SelectedItem as MyComboBoxItem)?.Content?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModMain.Hint("请先选择角色", ModMain.HintType.Critical);
            return;
        }

        BtnLaunchProxy.IsEnabled = false;
        try
        {
            OpenNelProxyLaunchResult result = await Task.Run(() =>
                OpenNelProxyService.LaunchGame(_selectedServer.EntityId, _selectedServer.Name, roleName));
            if (!result.Success)
            {
                ModMain.Hint(string.IsNullOrWhiteSpace(result.Message) ? "启动代理失败" : result.Message,
                    ModMain.HintType.Critical);
                return;
            }

            LabRoleStatus.Text = string.IsNullOrWhiteSpace(result.LocalAddress)
                ? $"MITM 代理已启动，标识: {result.Identifier}"
                : $"MITM 代理已启动: {result.LocalAddress}";
            ModMain.Hint($"本地代理入口 {result.LocalAddress}，请在 Minecraft 中连接此地址", ModMain.HintType.Finish);
            await RefreshSessionsAsync();
        }
        finally
        {
            BtnLaunchProxy.IsEnabled = true;
        }
    }

    private async Task RefreshSessionsAsync()
    {
        if (_isLoadingSessions)
            return;

        _isLoadingSessions = true;
        LoadSessions.Visibility = Visibility.Visible;
        PanSessions.Children.Clear();
        LabSessionEmpty.Visibility = Visibility.Collapsed;

        try
        {
            IReadOnlyList<OpenNelProxySession> sessions = await Task.Run(OpenNelProxyService.QuerySessions);
            if (sessions.Count == 0)
            {
                LabSessionEmpty.Visibility = Visibility.Visible;
                return;
            }

            foreach (var session in sessions)
                PanSessions.Children.Add(CreateSessionCard(session));
        }
        finally
        {
            LoadSessions.Visibility = Visibility.Collapsed;
            _isLoadingSessions = false;
        }
    }

    private UIElement CreateSessionCard(OpenNelProxySession session)
    {
        var card = new MyCard
        {
            Title = session.ServerName,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(16, 42, 16, 16)
        };
        panel.Children.Add(new TextBlock
        {
            Text = $"角色：{session.CharacterName}",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = $"状态：{session.StatusText}",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = $"入口：{session.LocalAddress}",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.75
        });

        var stopButton = new MyButton
        {
            Height = 32,
            Margin = new Thickness(0, 12, 0, 0),
            Text = "停止会话"
        };
        stopButton.Click += async (_, _) =>
        {
            stopButton.IsEnabled = false;
            try
            {
                await Task.Run(() => OpenNelProxyService.ShutdownSessions([session.Id]));
                await RefreshSessionsAsync();
            }
            finally
            {
                stopButton.IsEnabled = true;
            }
        };

        panel.Children.Add(stopButton);
        card.Children.Add(panel);
        return card;
    }
}
