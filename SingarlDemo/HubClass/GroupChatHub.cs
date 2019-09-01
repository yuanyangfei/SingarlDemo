﻿using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace SingarlDemo.HubClass
{
    /// <summary>
    /// SignalR Hub 群聊类
    /// </summary>
    [HubName("groupChatHub")] // 标记名称供js调用
    public class GroupChatHub : Hub
    {
        /// <summary>
        /// 用户名
        /// </summary>
        private string UserName
        {
            get
            {
                var userName = Context.RequestCookies["USERNAME"];
                return userName == null ? "" : HttpUtility.UrlDecode(userName.Value);
            }
        }

        /// <summary>
        /// 在线用户
        /// </summary>
        private static Dictionary<string, int> _onlineUser = new Dictionary<string, int>();

        /// <summary>
        /// 开始连接
        /// </summary>
        /// <returns></returns>
        public override Task OnConnected()
        {
            Connected();
            return base.OnConnected();
        }


        /// <summary>
        /// 重新链接
        /// </summary>
        /// <returns></returns>
        public override Task OnReconnected()
        {
            Connected();
            return base.OnReconnected();
        }



        private void Connected()
        {
            // 处理在线人员
            if (!_onlineUser.ContainsKey(UserName)) // 如果名称不存在，则是新用户
            {
                //例：这个项目管理员为渣渣辉,BOOS  渣渣辉进入这个页面时，就不用给自己发送信息；
                #region 定义发送群组
                List<MDS_User> UserList = new List<MDS_User>();
                MDS_User User = new MDS_User();
                User.UserName = "渣渣辉";
                User.UserCode = "001";
                UserList.Add(User);
                MDS_User User_ = new MDS_User();
                User_.UserName = "古天乐";
                User_.UserCode = "002";
                UserList.Add(User_);
                #endregion

                // string[] MDS_User = { "渣渣辉", "Boos" };
                //  MDS_User.Where(x=>x.)

               

                // 加入在线人员
                _onlineUser.Add(UserName, 1);

                var _user = UserList.Where(x => x.UserName != UserName).Select(x => new { key = x.UserName, value = x.UserCode });

                // 向客户端发送在线人员
                Clients.Caller.publshUser(_user.Select(i => i.key));
                var aa = string.Join("", _user.Select(x=>x.key).ToArray());
                // 向客户端发送加入聊天消息
                Clients.Groups(new List<string> { "GROUP-" + UserName, "GROUP-" + aa }).publshMsg(FormatMsg(UserName, "进入房间啦"));
              //  Clients.Group("GROUP-" + aa).publshMsg(FormatMsg(UserName, "加入聊天"));
               // Clients.Caller.publshMsg(FormatMsg("系统消息", UserName + "加入聊天"));
            }
            else
            {
                // 如果是已经存在的用户，则把在线链接的个数+1
                _onlineUser[UserName] = _onlineUser[UserName] + 1;
            }

            // 加入Hub Group，为了发送单独消息
            Groups.Add(Context.ConnectionId, "GROUP-" + UserName);
        }

        public class MDS_User {
            public string UserName { get; set; }
            public string UserCode { get; set; }
        }



        /// <summary>
        /// 结束连接
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            // 人员链接数-1
            _onlineUser[UserName] = _onlineUser[UserName] - 1;

            // 判断是否断开了所有的链接
            if (_onlineUser[UserName] == 0)
            {
                // 移除在线人员
                _onlineUser.Remove(UserName);

                // 向客户端发送在线人员
                Clients.All.publshUser(_onlineUser.Select(i => i.Key));

                // 向客户端发送退出聊天消息
               // Clients.All.publshMsg(FormatMsg("系统消息", UserName + "退出聊天"));
            }

            // 移除Hub Group
            Groups.Remove(Context.ConnectionId, "GROUP-" + UserName);
            return base.OnDisconnected(stopCalled);
        }

        /// <summary>
        /// 发送消息，供客户端调用
        /// </summary>
        /// <param name="user">用户名，如果为0，则是发送给所有人</param>
        /// <param name="msg">消息</param>
        //public void SendMsg(string user, string msg)
        //{
        //    if (user == "0")
        //    {
        //        // 发送给所有用户消息
        //        Clients.All.publshMsg(FormatMsg(UserName, msg));
        //    }
        //    else
        //    {
        //        //// 发送给自己消息
        //        //Clients.Group("GROUP-" + UserName).publshMsg(FormatMsg(UserName, msg));

        //        //// 发送给选择的人员
        //        //Clients.Group("GROUP-" + user).publshMsg(FormatMsg(UserName, msg));


        //        // 发送给自己消息
        //        Clients.Groups(new List<string> { "GROUP-" + UserName, "GROUP-" + user }).publshMsg(FormatMsg(UserName, msg));

        //    }
        //}


        /// <summary>
        /// 格式化发送的消息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private dynamic FormatMsg(string name, string msg)
        {
            return new { Name = name, Msg = HttpUtility.HtmlEncode(msg), Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
        }
    }


    /// <summary>
    /// 主动发送给用户消息，单例模式
    /// </summary>
    public class GroupChat
    {
        /// <summary>
        /// Clients，用来主动发送消息
        /// </summary>
        private IHubConnectionContext<dynamic> Clients { get; set; }

        private readonly static GroupChat _instance = new GroupChat(GlobalHost.ConnectionManager.GetHubContext<GroupChatHub>().Clients);

        private GroupChat(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
        }

        public static GroupChat Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// 主动给所有人发送消息，系统直接调用
        /// </summary>
        /// <param name="msg"></param>
        //public void SendSystemMsg(string msg)
        //{
        //    Clients.All.publshMsg(new { Name = "系统消息", Msg = msg, Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        //}
    }
}