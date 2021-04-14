using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using Impact.v2;
using Impact.v2.Commands;
using Impact;

namespace MyBot.Plugins.Roles
{
    [PluginName("AutoRole")]
    public class AutoRoleController : Autoload
    {
        private Dictionary<ulong, Dictionary<IEmote, (string, string)>> _autoRoles;
        
        public override void Load(Bot bot)
        {
            using (var guilds = Bot.Client.Guilds.GetEnumerator())
            {
                while (guilds.MoveNext())
                {
                    guilds.Current?.DownloadUsersAsync().GetAwaiter();
                }
            }

            _autoRoles = new Dictionary<ulong, Dictionary<IEmote, (string, string)>>();
            
            LoadData();
            
            bot.OnReactionAddedEvent += OnReactionAddedEvent;
            bot.OnReactionRemovedEvent += OnReactionAddedEvent;
        }

        private void LoadData()
        {
            using (Stream r = new FileStream("./autorole.dat", FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
            {
                var arr = new byte[sizeof(int)];
                r.Read(arr, 0, sizeof(int));

                var count = BitConverter.ToInt32(arr, 0);
                
                _autoRoles = new Dictionary<ulong, Dictionary<IEmote, (string, string)>>(count);
                
                for (var i = 0; i < count; i++)
                {
                    arr = new byte[sizeof(ulong)];
                    r.Read(arr, 0, sizeof(ulong));

                    var msgid = BitConverter.ToUInt64(arr, 0);
                    
                    
                    arr = new byte[sizeof(int)];
                    r.Read(arr, 0, sizeof(int));

                    var emojiCount = BitConverter.ToInt32(arr, 0);
                    
                    _autoRoles.Add(msgid, new Dictionary<IEmote, (string, string)>(emojiCount));
                    
                    for (var ii = 0; ii < emojiCount; ii++)
                    {
                        arr = new byte[sizeof(int)];
                        r.Read(arr, 0, sizeof(int));
                        
                        var emojiLength = BitConverter.ToInt32(arr, 0);
                        
                        arr = new byte[emojiLength];
                        r.Read(arr, 0, emojiLength);
                        
                        var emoji = Encoding.UTF32.GetString(arr);
                        var emote = new Emoji(emoji);
                        
                        arr = new byte[sizeof(int)];
                        r.Read(arr, 0, sizeof(int));
                        
                        var roleLength = BitConverter.ToInt32(arr, 0);
                        
                        arr = new byte[roleLength];
                        r.Read(arr, 0, roleLength);
                        
                        var role = Encoding.ASCII.GetString(arr);
                        
                        _autoRoles[msgid].Add(emote, (emoji, role));
                    }
                }
            }
        }
        
        private void SaveData()
        {
            using (Stream r = new FileStream("./autorole.dat", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                var arr = BitConverter.GetBytes(_autoRoles.Count);
                r.Write(arr, 0, arr.Length);
                
                foreach (var keyValuePair in _autoRoles)
                {
                    // ulong
                    arr = BitConverter.GetBytes(keyValuePair.Key);
                    r.Write(arr, 0, arr.Length);
                    
                    // Emoji count
                    arr = BitConverter.GetBytes(keyValuePair.Value.Count);
                    r.Write(arr, 0, arr.Length);
                    
                    foreach (var valuePair in keyValuePair.Value)
                    {
                        
                        // emoji unicode
                        arr = Encoding.UTF32.GetBytes(valuePair.Value.Item1);
                        
                        // emoji length
                        var arr2 = BitConverter.GetBytes(arr.Length);
                        
                        r.Write(arr2, 0, arr2.Length);
                        r.Write(arr, 0, arr.Length);
                        
                        // role length
                        arr = BitConverter.GetBytes(valuePair.Value.Item2.Length);
                        r.Write(arr, 0, arr.Length);
                        
                        // role unicode
                        arr = Encoding.ASCII.GetBytes(valuePair.Value.Item2);
                        r.Write(arr, 0, arr.Length);
                    }
                }
            }
        }

        private void OnReactionAddedEvent(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction, bool added)
        {
            if (!_autoRoles.ContainsKey(reaction.MessageId))
            {
                return;
            }
            
            if (added)
            {
                if (!_autoRoles[reaction.MessageId].ContainsKey(reaction.Emote))
                {
                    return;
                }
                
                IRole role = (channel as IGuildChannel)?.Guild.Roles.FirstOrDefault(x => x.Name == _autoRoles[reaction.MessageId][reaction.Emote].Item2);
                
                var guild = Bot.Client.GetGuild(((IGuildChannel) channel).GuildId);
                var user = guild.GetUser(reaction.UserId);
                
                user.AddRoleAsync(role);
            }
            else
            {
                if (!_autoRoles[reaction.MessageId].ContainsKey(reaction.Emote))
                {
                    return;
                }
                
                IRole role = (channel as IGuildChannel)?.Guild.Roles.FirstOrDefault(x => x.Name == _autoRoles[reaction.MessageId][reaction.Emote].Item2);
                
                var guild = Bot.Client.GetGuild(((IGuildChannel) channel).GuildId);
                var user = guild.GetUser(reaction.UserId);
                
                user.RemoveRoleAsync(role);
                //(reaction.User.Value as IGuildUser)?.RemoveRoleAsync(role);
            }
        }

        public void Add(ulong msgId, IEmote emote, string emoji, string role)
        {
            if (_autoRoles.ContainsKey(msgId))
            {
                if (!_autoRoles[msgId].ContainsKey(emote))
                {
                    _autoRoles[msgId].Add(emote, (emoji, role));
                }
            }
            else
            {
                var d = new Dictionary<IEmote, (string, string)>
                {
                    {emote, (emoji, role)}
                };

                _autoRoles.Add(msgId, d);

                SaveData();
            }
        }
    }
}