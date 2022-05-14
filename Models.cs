using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSO_FriendStatus.Models
{
    public class Result
    {
        public Friend[] friends { get; set; }
    }

    public class FriendGroup : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
        public bool IsOnline { get; set; }
        public ObservableCollection<Friend> Friends { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Friend
    {
        public long id { get; set; }
        public string nsaId { get; set; }
        public string imageUri { get; set; }
        public string name { get; set; }
        public bool isFriend { get; set; }
        public bool isFavoriteFriend { get; set; }
        public bool isServiceUser { get; set; }
        public int friendCreatedAt { get; set; }
        public Presence presence { get; set; }

        public void Update(Friend newData)
        {
            id = newData.id;
            nsaId = newData.nsaId;
            imageUri = newData.imageUri;
            name = newData.name;
            isFriend = newData.isFriend;
            isFavoriteFriend = newData.isFavoriteFriend;
            isServiceUser = newData.isServiceUser;
            friendCreatedAt = newData.friendCreatedAt;
            presence = newData.presence;
        }

        private string GetTimeText(long seconds)
        {
            return seconds switch
            {
                long i when i < 60 => $"{i}秒",
                long i when i < 60 * 60 => $"{i / 60}分",
                long i when i < 60 * 60 * 24 => $"{i / 60 / 60}時間",
                _ => $"{seconds / 60 / 60 / 24}日",
            };
        }

        public string OnlineState
        {
            get
            {
                if (presence.IsOnline)
                {
                    return $"● {presence.game.name}";
                }

                if (presence.logoutAt != 0)
                {
                    var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                    return $"最後のオンライン: {GetTimeText(now - presence.logoutAt)}前";
                }

                return "オフライン";
            }
        }

        public Brush OnlineStateBrush
        {
            get
            {
                return presence.IsOnline ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
            }
        }
    }

    public class Presence
    {
        public string state { get; set; }
        public int updatedAt { get; set; }
        public int logoutAt { get; set; }
        public Game game { get; set; }

        public bool IsOnline => state == "ONLINE" || state == "PLAYING";
    }

    public class Game
    {
        public string name { get; set; }
        public string imageUri { get; set; }
        public string shopUri { get; set; }
        public int totalPlayTime { get; set; }
        public int firstPlayedAt { get; set; }
        public string sysDescription { get; set; }
    }
}
