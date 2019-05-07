using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Discord;
using Discord.WebSocket;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    public class BanCommands
    {
        public List<Command> GetBanCommands()
        {
            List<Command> banCommands = new List<Command>();

            Command globalBan = new Command("globalBan");
            globalBan.RequiredPermission = Command.PermissionLevels.BotOwner;
            globalBan.Description = "Ban someone from every server the bot is currently on";
            globalBan.ToExecute += async (client, msg, parameters) =>
            {
                if (!ulong.TryParse(parameters[0], out ulong userId))
                {
                    await msg.ReplyAsync($"Invalid UserId");
                    return;
                }

                if (parameters.Count <= 1)
                {
                    await msg.ReplyAsync($"Need a reasono and/or userId");
                    return;
                }

                string reason = $"Globally banned for: {parameters.reJoin()}";

                int succ = 0, fail= 0 , opt = 0;
                GenericBot.GlobalConfiguration.BlacklistedIds.Add(userId);
                foreach (var guild in client.Guilds)
                {
                    if (GenericBot.GuildConfigs[guild.Id].GlobalBanOptOut)
                    {
                        opt++;
                        continue;
                    }

                    try
                    {
                        await guild.AddBanAsync(userId, 0, reason);
                        succ++;
                    }
                    catch
                    {
                        fail++;
                    }
                }

                string repl =
                    $"Result: Banned `{msg.GetGuild().GetBansAsync().Result.First(b => b.User.Id == userId).User}` for `{reason}`\n";
                repl += $"\nSuccesses: `{succ}`\nFailures: `{fail}`\nOpt-Outs: `{opt}`";

                await msg.ReplyAsync(repl);

            };

            banCommands.Add(globalBan);

            Command unban = new Command("unban");
            unban.Description = "Unban a user given their ID";
            unban.RequiredPermission = Command.PermissionLevels.Moderator;
            unban.Usage = "unban <userId";
            unban.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync("Please specify a userid to unban");
                    return;
                }
                GuildConfig gc = GenericBot.GuildConfigs[msg.GetGuild().Id];
                if(ulong.TryParse(parameters[0], out ulong bannedUId) && gc.Bans.HasElement(b => b.Id == bannedUId, out GenericBan banToRemove))
                {
                    gc.Bans.Remove(banToRemove);
                    gc.Save();
                    try
                    {
                        var user = msg.GetGuild().GetBansAsync().Result.First(b => b.User.Id == bannedUId).User;
                        await msg.GetGuild().RemoveBanAsync(bannedUId);
                        await msg.ReplyAsync($"Succesfully unbanned `{user}` (`{user.Id}`)");

                        var builder = new EmbedBuilder()
                            .WithTitle("User Unbanned")
                            .WithDescription($"Banned for: {banToRemove.Reason}")
                            .WithColor(new Color(0xFFFF00))
                            .WithFooter(footer => {
                                footer
                                    .WithText($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                            })
                            .WithAuthor(author => {
                                author
                                    .WithName(user.ToString())
                                    .WithIconUrl(user.GetAvatarUrl());
                            })
                            .AddField(new EmbedFieldBuilder().WithName("All Warnings").WithValue(
                                new DBGuild(msg.GetGuild().Id).GetOrCreateUser(user.Id).Warnings.SumAnd()));

                        await ((SocketTextChannel)GenericBot.DiscordClient.GetChannel(gc.UserLogChannelId))
                            .SendMessageAsync("", embed: builder.Build());
                    }
                    catch (Discord.Net.HttpException httpException)
                    {
                        await GenericBot.Logger.LogErrorMessage(httpException.Message + "\n" + httpException.StackTrace);
                        await msg.ReplyAsync("Could not unban that user. Either I don't have the permissions or they weren't banned");
                    }
                }
            };

            banCommands.Add(unban);

            Command ban = new Command("ban");
            ban.Description = "Ban a user from the server, whether or not they're on it";
            ban.Delete = false;
            ban.RequiredPermission = Command.PermissionLevels.Moderator;
            ban.Usage = $"{ban.Name} <user> <time> <reason>";
            ban.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                ulong uid;
                if (ulong.TryParse(parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out uid))
                {
                    if (uid == client.GetApplicationInfoAsync().Result.Owner.Id)
                    {
                        await msg.ReplyAsync("Haha lol no");
                        return;
                    }

                    parameters.RemoveAt(0);
                    var time = new DateTimeOffset();

                    try
                    {
                        if (parameters[0] == "0" || parameters[0] == "0d")
                            time = DateTimeOffset.MaxValue; 
                        else
                            time = parameters[0].ParseTimeString();
                        parameters.RemoveAt(0);
                    }
                    catch (System.FormatException ex)
                    { time = DateTimeOffset.MaxValue; }
                    var tmsg = time == DateTimeOffset.MaxValue ? "permanently" : $"for `{(time.AddSeconds(1) - DateTimeOffset.UtcNow).FormatTimeString()}`"; 

                    string reason = parameters.reJoin();
                    var bans = msg.GetGuild().GetBansAsync().Result;
                    if (bans.Any(b => b.User.Id == uid))
                    {
                        await msg.ReplyAsync(
                            $"`{bans.First(b => b.User.Id == uid).User}` is already banned for `{bans.First(b => b.User.Id == uid).Reason}`");
                    }
                    else
                    {
                        bool dmSuccess = true;
                        string dmMessage = $"You have been banned from **{msg.GetGuild().Name}** ";
                        dmMessage += tmsg;
                        if(!string.IsNullOrEmpty(reason))
                            dmMessage += $" for the following reason: \n\n{reason}\n\n";
                        try
                        {
                            await msg.GetGuild().GetUser(uid).GetOrCreateDMChannelAsync().Result
                                .SendMessageAsync(dmMessage);
                        }
                        catch
                        {
                            dmSuccess = false;
                        }

                        try
                        {
                            string areason = reason.Replace("\"", "'");
                            if (areason.Length > 256)
                            {
                                areason = areason.Substring(0, 250) + "...";
                            }
                            await msg.GetGuild().AddBanAsync(uid, reason: areason);
                        }
                        catch
                        {
                            await msg.ReplyAsync($"Could not ban the given user. Try checking role hierarchy and permissions");
                            return;
                        }

                        bans = msg.GetGuild().GetBansAsync().Result;
                        var user = bans.First(u => u.User.Id == uid).User;
                        string banMessage = $"Banned `{user}` (`{user.Id}`)";
                        if (string.IsNullOrEmpty(reason))
                            banMessage += $" 👌";
                        else
                            banMessage += $" for `{reason}`";
                        banMessage += $"{tmsg} 👌"; 

                        if (!dmSuccess) banMessage += "\nThe user could not be messaged";

                        var builder = new EmbedBuilder()
                            .WithTitle("User Banned")
                            .WithDescription(banMessage)
                            .WithColor(new Color(0xFFFF00))
                            .WithFooter(footer => {
                                footer
                                    .WithText($"By {msg.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                            })
                            .WithAuthor(author => {
                                author
                                    .WithName(user.ToString())
                                    .WithIconUrl(user.GetAvatarUrl());
                            });

                        var guilddb = new DBGuild(msg.GetGuild().Id);
                        var guildconfig = GenericBot.GuildConfigs[msg.GetGuild().Id];
                        guildconfig.Bans.Add(
                            new GenericBan(user.Id, msg.GetGuild().Id, reason, time));
                        guildconfig.ProbablyMutedUsers.Remove(user.Id);
                        string t = tmsg;

                        guildconfig.Save();
                        var bannedUser = guilddb.GetOrCreateUser(user.Id)
                            .AddWarning(
                                $"Banned {t} for `{reason}` (By `{msg.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                        guilddb.AddOrUpdateUser(bannedUser);

                        await msg.Channel.SendMessageAsync("", embed: builder.Build());
                        if (guildconfig.UserLogChannelId != 0)
                        {
                            await (client.GetChannel(guildconfig.UserLogChannelId) as SocketTextChannel)
                                .SendMessageAsync("", embed: builder.Build());
                        }
                    }
                }
                else
                {
                    await msg.ReplyAsync("Try specifying someone to ban first");
                }
            };

            banCommands.Add(ban);

            Command pban = new Command("purgeban");
            pban.Description = "Ban a user from the server, whether or not they're in it, and delete the last day of their  messages";
            pban.Delete = false;
            pban.RequiredPermission = Command.PermissionLevels.Moderator;
            pban.Usage = $"{pban.Name} <user> <time in days> <reason>";
            pban.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                ulong uid;
                if (ulong.TryParse(parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out uid))
                {
                    if (uid == client.GetApplicationInfoAsync().Result.Owner.Id)
                    {
                        await msg.ReplyAsync("Haha lol no");
                        return;
                    }

                    parameters.RemoveAt(0);
                    var time = new DateTimeOffset();

                    try
                    {
                        if (parameters[0] == "0" || parameters[0] == "0d")
                            time = DateTimeOffset.MaxValue;
                        else
                            time = parameters[0].ParseTimeString();
                        parameters.RemoveAt(0);
                    }
                    catch (System.FormatException ex)
                    { time = DateTimeOffset.MaxValue; }
                
                    var tmsg = time == DateTimeOffset.MaxValue ? "permanently" : $"for `{(time.AddSeconds(1) - DateTimeOffset.UtcNow).FormatTimeString()}`";

                    string reason = parameters.reJoin();
                    var bans = msg.GetGuild().GetBansAsync().Result;
                    if (bans.Any(b => b.User.Id == uid))
                    {
                        await msg.ReplyAsync(
                            $"`{bans.First(b => b.User.Id == uid).User}` is already banned for `{bans.First(b => b.User.Id == uid).Reason}`");
                    }
                    else
                    {
                        bool dmSuccess = true;
                        string dmMessage = $"You have been banned from **{msg.GetGuild().Name}** ";
                        dmMessage += tmsg;
                        if (!string.IsNullOrEmpty(reason))
                            dmMessage += $" for the following reason: \n\n{reason}\n\n";
                        try
                        {
                            await msg.GetGuild().GetUser(uid).GetOrCreateDMChannelAsync().Result
                                .SendMessageAsync(dmMessage);
                        }
                        catch
                        {
                            dmSuccess = false;
                        }

                        try
                        {
                            string areason = reason.Replace("\"", "'");
                            if (areason.Length > 256)
                            {
                                areason = areason.Substring(0, 250) + "...";
                            }
                            await msg.GetGuild().AddBanAsync(uid, reason: areason, pruneDays: 1);
                        }
                        catch
                        {
                            await msg.ReplyAsync($"Could not ban the given user. Try checking role hierarchy and permissions");
                            return;
                        }

                        bans = msg.GetGuild().GetBansAsync().Result;
                        var user = bans.First(u => u.User.Id == uid).User;
                        string banMessage = $"Banned `{user}` (`{user.Id}`)";
                        if (string.IsNullOrEmpty(reason))
                            banMessage += $" 👌";
                        else
                            banMessage += $" for `{reason}`";
                        banMessage += $"{tmsg} 👌";

                        if (!dmSuccess) banMessage += "\nThe user could not be messaged";

                        var builder = new EmbedBuilder()
                            .WithTitle("User Banned")
                            .WithDescription(banMessage)
                            .WithColor(new Color(0xFFFF00))
                            .WithFooter(footer => {
                                footer
                                    .WithText($"By {msg.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                            })
                            .WithAuthor(author => {
                                author
                                    .WithName(user.ToString())
                                    .WithIconUrl(user.GetAvatarUrl());
                            });

                        var guilddb = new DBGuild(msg.GetGuild().Id);
                        var guildconfig = GenericBot.GuildConfigs[msg.GetGuild().Id];
                        guildconfig.Bans.Add(
                            new GenericBan(user.Id, msg.GetGuild().Id, reason, time));
                        guildconfig.ProbablyMutedUsers.Remove(user.Id);
                        string t = tmsg;

                        guildconfig.Save();
                        var bannedUser = guilddb.GetOrCreateUser(user.Id)
                            .AddWarning(
                                $"Banned {t} for `{reason}` (By `{msg.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                        guilddb.AddOrUpdateUser(bannedUser);

                        await msg.Channel.SendMessageAsync("", embed: builder.Build());
                        if (guildconfig.UserLogChannelId != 0)
                        {
                            await (client.GetChannel(guildconfig.UserLogChannelId) as SocketTextChannel)
                                .SendMessageAsync("", embed: builder.Build());
                        }
                    }
                }
                else
                {
                    await msg.ReplyAsync("Try specifying someone to ban first");
                }
            };

            banCommands.Add(pban);


            Command kick = new Command("kick");
            kick.Description = "kick a user from the server, whether or not they're on it";
            kick.Delete = false;
            kick.RequiredPermission = Command.PermissionLevels.Moderator;
            kick.Usage = $"{kick.Name} <user> <reason>";
            kick.ToExecute += async (client, msg, parameters) =>
            {
                if (!msg.GetMentionedUsers().Any())
                {
                    await msg.ReplyAsync($"You need to specify a user to kick");
                    return;
                }

                var user = msg.GetMentionedUsers().First();
                parameters.RemoveAt(0);
                if (user.Id == client.GetApplicationInfoAsync().Result.Owner.Id)
                {
                    await msg.ReplyAsync("Haha lol no");
                    return;
                }

                string reason = parameters.reJoin();

                bool dmSuccess = true;
                string dmMessage = $"You have been kicked from **{msg.GetGuild().Name}**";
                if(!string.IsNullOrEmpty(reason))
                    dmMessage += $" for the following reason: \n\n{reason}\n\n";
                try
                {
                    await msg.GetGuild().GetUser(user.Id).GetOrCreateDMChannelAsync().Result
                        .SendMessageAsync(dmMessage);
                }
                catch
                {
                    dmSuccess = false;
                }

                try
                {
                    await msg.GetGuild().GetUser(user.Id).KickAsync();
                }
                catch
                {
                    await msg.ReplyAsync($"Could not ban the given user. Try checking role hierarchy and permissions");
                    return;
                }

                string kickMessage = $"Kicked `{user}` (`{user.Id}`)";
                if (!string.IsNullOrEmpty(reason))
                    kickMessage += $" for `{reason}`";

                if (!dmSuccess) kickMessage += "\nThe user could not be messaged";

                var builder = new EmbedBuilder()
                    .WithTitle("User Kicked")
                    .WithDescription(kickMessage)
                    .WithColor(new Color(0xFFFF00))
                    .WithFooter(footer => {
                        footer
                            .WithText($"By {msg.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                    })
                    .WithAuthor(author => {
                        author
                            .WithName(user.ToString())
                            .WithIconUrl(user.GetAvatarUrl());
                    });

                var guilddb = new DBGuild(msg.GetGuild().Id);
                var guildconfig = GenericBot.GuildConfigs[msg.GetGuild().Id];
                guildconfig.ProbablyMutedUsers.Remove(user.Id);
                guildconfig.Save();
                var kickedUser = guilddb.GetOrCreateUser(user.Id)
                    .AddWarning(
                        $"Kicked for `{reason}` (By `{msg.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                guilddb.AddOrUpdateUser(kickedUser);

                await msg.Channel.SendMessageAsync("", embed: builder.Build());
                if (guildconfig.UserLogChannelId != 0)
                {
                    await (client.GetChannel(guildconfig.UserLogChannelId) as SocketTextChannel)
                        .SendMessageAsync("", embed: builder.Build());
                }
            };

            banCommands.Add(kick);


            return banCommands;
        }
    }
}
