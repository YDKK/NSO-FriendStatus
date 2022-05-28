using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using NSO_FriendStatus.Models;
using System.Text.Json;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using System.Diagnostics;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NSO_FriendStatus
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const int UpdateIntervalSeconds = 60;
        private const bool EnableAutoUpdate = true;
        private const bool EnableOnlineNotify = true;

        private List<FriendGroup> CurrentFriends = new();
        private FriendGroup CurrentOnlineFriends;
        private FriendGroup CurrentOfflineFriends;

        private readonly DispatcherTimer Timer;

        public MainWindow()
        {
            InitializeComponent();

            CurrentOnlineFriends = new FriendGroup
            {
                Friends = new ObservableCollection<Friend>(),
                IsOnline = true,
                Name = "オンライン (0)"
            };
            CurrentFriends.Add(CurrentOnlineFriends);

            CurrentOfflineFriends = new FriendGroup
            {
                Friends = new ObservableCollection<Friend>(),
                IsOnline = false,
                Name = "オフライン (0)"
            };
            CurrentFriends.Add(CurrentOfflineFriends);

            Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds)
            };
            Timer.Tick += (sender, e) =>
            {
                UpdateFriends();
            };

            if (EnableAutoUpdate)
            {
                Timer.Start();
            }

            UpdateFriends();
        }

        private void UpdateFriends()
        {
            Friend[] onlineFriends;
            Friend[] offlineFriends;
            string json = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    FileName = "nxapi.cmd",
                    Arguments = "nso friends --json"
                };
                var process = Process.Start(psi);

                json = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var allFriends = JsonDocument.Parse(json).RootElement.EnumerateArray().Select(x => x.Deserialize<Friend>()).ToArray();
                onlineFriends = allFriends.Where(x => x.presence.IsOnline).ToArray();
                offlineFriends = allFriends.Where(x => !x.presence.IsOnline).ToArray();
            }
            catch (Exception ex)
            {
                //TODO
                Debug.WriteLine(json);
                Debug.WriteLine(ex);
                Debug.WriteLine(ex.Message);
                return;
            }

            if (EnableOnlineNotify)
            {
                var newOnlineFriends = onlineFriends.Where(x => !CurrentOnlineFriends.Friends.Any(z => z.nsaId == x.nsaId) == true);
                foreach (var newOnlineFriend in newOnlineFriends)
                {
                    new ToastContentBuilder()
                        .AddArgument("action", "showUser")
                        .AddArgument("nsaId", newOnlineFriend.nsaId)
                        .AddAppLogoOverride(new Uri(newOnlineFriend.imageUri), ToastGenericAppLogoCrop.Circle)
                        .AddText(newOnlineFriend.name, hintMaxLines: 1)
                        .AddText("オンライン")
                        .AddText(newOnlineFriend.presence.game.name)
                        .Show();
                }
            }

            foreach (var (current, @new) in new[] { (CurrentOnlineFriends, onlineFriends), (CurrentOfflineFriends, offlineFriends) })
            {
                if (!@new.Any())
                {
                    current.Friends.Clear();
                    continue;
                }

                for (int i = 0; i < current.Friends.Count;)
                {
                    if (!@new.Any(x => x.nsaId == current.Friends[i].nsaId))
                    {
                        current.Friends.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                for (int i = 0; i < @new.Length; i++)
                {
                    var friend = @new[i];
                    if (current.Friends.SingleOrDefault(x => x.nsaId == friend.nsaId) is Friend item)
                    {
                        if (current.Friends.IndexOf(item) == i)
                        {
                            if (item.presence.updatedAt != friend.presence.updatedAt)
                            {
                                current.Friends[i] = friend;
                            }
                        }
                        else
                        {
                            current.Friends.Remove(item);
                            current.Friends.Insert(i, friend);
                        }
                    }
                    else
                    {
                        current.Friends.Insert(i, friend);
                    }
                }
            }

            var onlineNum = CurrentOnlineFriends.Friends.Count;
            var offlineNum = CurrentOfflineFriends.Friends.Count;

            CurrentOnlineFriends.Name = $"オンライン ({onlineNum})";
            CurrentOfflineFriends.Name = $"オフライン ({offlineNum})";
        }
    }
}
