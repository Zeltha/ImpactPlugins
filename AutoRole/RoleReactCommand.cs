using System;
using System.Collections.ObjectModel;
using Discord;
using Impact;
using Impact.Commands;
using Impact.Management;

namespace MyBot.Plugins.Roles
{
    [CommandName("Rolewatcher"), CommandString("!rolewatcher <msgid:[\\d]*> <emoji:string> <role:string>")]
    public class RoleReactCommand : Command
    {
        public override void Run(Bot bot, ReadOnlyDictionary<string, string> data, UserMessage msg)
        {
            var m =  msg.Message.Channel.GetMessageAsync(Convert.ToUInt64(data["msgid"]));

            m.GetAwaiter();

            var r = m.Result;
            
            IEmote i = new Emoji(data["emoji"]);

            r.AddReactionAsync(i);

            bot.GetAutoloaded<AutoRoleController>("AutoRole").Add(m.Result.Id, i, data["emoji"], data["role"]);

            msg.Message.DeleteAsync();
        }
    }
}