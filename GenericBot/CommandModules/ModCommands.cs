using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GenericBot.Entities;
using Newtonsoft.Json;

namespace GenericBot.CommandModules
{
    public class ModCommands
    {
        public List<Command> GetModCommands()
        {
            List<Command> ModCommands = new List<Command>();

            Command clear = new Command("clear");
            clear.Description = "Clear a number of messages from a channel";
            clear.Usage = "clear <number> <user>";
            clear.RequiredPermission = Command.PermissionLevels.Moderator;
            clear.ToExecute += async (client, msg, paramList) =>
            {
                if (paramList.Empty())
                {
                    await msg.ReplyAsync("You gotta tell me how many messages to delete!");
                    return;
                }

                ulong count;
               

                if (ulong.TryParse(paramList[0], out count))
                {
                    int messagesToDownloadCount = (int) Math.Min(1000, count);
                    List<IMessage> msgs = (msg.Channel as SocketTextChannel).GetManyMessages(messagesToDownloadCount);
                    if (msg.MentionedUsers.Any())
                    {
                        var users = msg.MentionedUsers;
                        msgs = msgs.Where(m => users.Select(u => u.Id).Contains(m.Author.Id)).ToList();
                        msgs.Add(msg);
                    }
                    if (paramList.Count > 1 && !msg.MentionedUsers.Any())
                    {
                        await msg.ReplyAsync($"It looks like you're trying to mention someone but failed.");
                        return;
                    }
                    if(count > 1000) // If the input number was probably an ID
                    {
                        msgs = msgs.Where(m => m.Id >= count).ToList(); // Only keep messages sent after that ID
                    }
                    msgs = msgs.Where(m => DateTime.Now - m.CreatedAt < TimeSpan.FromDays(14)).ToList(); // Only keep last 2 weeks of messages (API Limits)
                    var logChannelId = GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId;
                    if(msg.GetGuild().Channels.Any(c => c.Id == logChannelId))
                    {
                        string fileName = $"files/{msg.Channel.Name}-cleared-{msg.Id.ToString().Substring(14, 4)}.txt";
                        File.WriteAllText(fileName, JsonConvert.SerializeObject(new
                        {
                            Guild = new
                            {
                                msg.GetGuild().Id,
                                msg.GetGuild().Name, 
                                Channel = new
                                {
                                    msg.Channel.Id,
                                    msg.Channel.Name
                                }
                            },
                            Messages = msgs.OrderBy(m => m.Id).Select(m => new
                            {
                                m.Id,
                                Author = new
                                {
                                    m.Author.Id,
                                    Username = $"{m.Author.Username}#{m.Author.Discriminator}"
                                },
                                m.Content,
                                Attatchments = m.Attachments,
                                Timestamp = m.CreatedAt
                            })
                        }, Formatting.Indented));
                        msg.GetGuild().GetTextChannel(logChannelId).SendFileAsync(fileName, "");
                        File.Delete(fileName);
                    }
                    msgs.ForEach(m => GenericBot.ClearedMessageIds.Add(m.Id));

                    await (msg.Channel as ITextChannel).DeleteMessagesAsync(msgs);

                    var messagesSent = new List<IMessage>();

                    messagesSent.Add(msg.ReplyAsync($"{msg.Author.Mention}, done deleting those messages!").Result);
                    if (msgs.Any(m => DateTime.Now - m.CreatedAt > TimeSpan.FromDays(14)))
                    {
                        messagesSent.Add(msg.ReplyAsync($"I couldn't delete all of them, some were older than 2 weeks old :frowning:").Result);
                    }

                    await Task.Delay(2500);
                    await (msg.Channel as ITextChannel).DeleteMessagesAsync(messagesSent);
                }
                else
                {
                    await msg.ReplyAsync("That's not a valid number");
                }
            };

            ModCommands.Add(clear);

            Command whois = new Command("whois");
            whois.Description = "Get information about a user currently on the server from a ID or Mention";
            whois.Usage = "whois @user";
            whois.RequiredPermission = Command.PermissionLevels.Moderator;
            whois.ToExecute += async (client, msg, parameters) =>
            {
                await msg.GetGuild().DownloadUsersAsync();
                if (!msg.GetMentionedUsers().Any())
                {
                    await msg.ReplyAsync("No user found");
                    return;
                }
                SocketGuildUser user;
                ulong uid = msg.GetMentionedUsers().FirstOrDefault().Id;

                if (msg.GetGuild().Users.Any(u => u.Id == uid))
                {
                    user = msg.GetGuild().GetUser(uid);
                }
                else
                {
                    await msg.ReplyAsync("User not found");
                    return;
                }

                string nickname = msg.GetGuild().Users.All(u => u.Id != uid) || string.IsNullOrEmpty(user.Nickname) ? "None" : user.Nickname;
                string roles = "";
                foreach (var role in user.Roles.Where(r => !r.Name.Equals("@everyone")).OrderByDescending(r => r.Position))
                {
                    roles += $"`{role.Name}`, ";
                }
                DBUser dbUser = new DBGuild(msg.GetGuild().Id).GetOrCreateUser(user.Id);

                string nicks = "", usernames = "";
                if (!dbUser.Usernames.Empty())
                {
                    foreach (var str in dbUser.Usernames.Distinct().ToList())
                    {
                        usernames += $"`{str.Replace('`', '\'')}`, ";
                    }
                }
                if (!dbUser.Nicknames.Empty())
                {
                    foreach (var str in dbUser.Nicknames.Distinct().ToList())
                    {
                        nicks += $"`{str.Replace('`', '\'')}`, ";
                    }
                }
                nicks = nicks.Trim(',', ' ');
                usernames = usernames.Trim(',', ' ');

                string info = $"User Id:  `{user.Id}`\n";
                info += $"Username: `{user.ToString()}`\n";
                info += $"Past Usernames: {usernames}\n";
                info += $"Nickname: `{nickname}`\n";
                if (!dbUser.Nicknames.Empty())
                    info += $"Past Nicknames: {nicks}\n";
                info += $"Created At: `{string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss zzzz}", user.CreatedAt.LocalDateTime)}GMT` " +
                        $"(about {(DateTime.UtcNow - user.CreatedAt).Days} days ago)\n";
                if (user.JoinedAt.HasValue)
                    info +=
                        $"Joined At: `{string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss zzzz}", user.JoinedAt.Value.LocalDateTime)}GMT`" +
                        $"(about {(DateTime.UtcNow - user.JoinedAt.Value).Days} days ago)\n";
                info += $"Roles: {roles.Trim(' ', ',')}\n";
                if (!dbUser.Warnings.Empty())
                    info += $"`{dbUser.Warnings.Count}` Warnings: {dbUser.Warnings.reJoin(" | ")}";
                info += $"\nAvatar: {user.GetAvatarUrl(size: 256)}";

                foreach (var str in info.SplitSafe(','))
                {
                    await msg.ReplyAsync(str.TrimStart(','));
                }
            };

            ModCommands.Add(whois);

            Command find = new Command("find");
            find.Description = "Get information about a user currently on the server from a ID or Mention";
            find.Usage = "whois @user";
            find.RequiredPermission = Command.PermissionLevels.Moderator;
            find.ToExecute += async (client, msg, parameters) =>
            {
                string input = parameters.reJoin();
                List<DBUser> foundUsers = new List<DBUser>();
                var guildDb = new DBGuild(msg.GetGuild().Id);

                if (msg.MentionedUsers.Any())
                {
                    foundUsers.Add(guildDb.GetOrCreateUser(msg.MentionedUsers.First().Id));
                }
                else if ((input.Length > 16 && input.Length < 19) && ulong.TryParse(input, out ulong id))
                {
                    foundUsers.Add(guildDb.GetOrCreateUser(id));
                }
                else if (input.Length < 3)
                {
                    await msg.ReplyAsync($"I can't search for that, it's dangerously short and risks a crash.");
                    return;
                }

                else
                {
                    foreach (var user in guildDb.Users)
                    {
                        try
                        {
                            if (!user.Nicknames.Empty())
                            {
                                if (user.Nicknames.Any(n => n.ToLower().Contains(input.ToLower())))
                                {
                                    foundUsers.Add(user);
                                }
                            }

                            if (!user.Usernames.Empty())
                            {
                                if (user.Usernames.Any(n => n.ToLower().Contains(input.ToLower())))
                                {
                                    foundUsers.Add(user);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            foreach (var u in guildDb.Users)
                            {
                                if (!u.Nicknames.Empty())
                                {
                                    user.Nicknames = user.Nicknames.Where(n => !string.IsNullOrEmpty(n)).ToList();
                                }

                                if (!u.Usernames.Empty())
                                {
                                    user.Usernames = user.Usernames.Where(n => !string.IsNullOrEmpty(n)).ToList();
                                }

                            }
                            if (new Command("test").GetPermissions(msg.Author, msg.GetGuild().Id) >= Command.PermissionLevels.GlobalAdmin)
                                await msg.ReplyAsync($"```\n{ex.Message}\n{ex.StackTrace}\n{user.ID} : {user.Usernames.Count} | {user.Nicknames.Count}\n```");
                        }
                    }
                    foundUsers = foundUsers.Distinct().ToList();
                }

                if (foundUsers.Count > 5)
                {
                    string info =
                        $"Found `{foundUsers.Count}` users. Their first stored usernames are:\n{foundUsers.Select(u => $"{u.Usernames.First().Escape()} (`{u.ID}`)").ToList().SumAnd()}" +
                        $"\nTry using more precise search parameters";

                    foreach (var str in info.SplitSafe(','))
                    {
                        await msg.ReplyAsync(str.TrimStart(','));
                    }

                    return;
                }
                else if (foundUsers.Count == 0)
                {
                    await msg.ReplyAsync($"No users found");
                }

                foreach (var dbUser in foundUsers)
                {
                    string nicks = "", usernames = "";
                    if (dbUser.Usernames != null && !dbUser.Usernames.Empty())
                    {
                        usernames = dbUser.Usernames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                    }

                    if (dbUser.Nicknames != null && !dbUser.Nicknames.Empty())
                    {
                        nicks = dbUser.Nicknames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                    }


                    string info = $"User: <@!{dbUser.ID}>\nUser Id:  `{dbUser.ID}`\n";
                    info += $"Past Usernames: {usernames}\n";
                    info += $"Past Nicknames: {nicks}\n";
                    SocketGuildUser user = msg.GetGuild().GetUser(dbUser.ID);
                    if (user != null && user.Id != msg.Author.Id)
                    {
                        info +=
                            $"Created At: `{string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss zzzz}", user.CreatedAt.LocalDateTime)}GMT` " +
                            $"(about {(DateTime.UtcNow - user.CreatedAt).Days} days ago)\n";
                    }

                    if (user != null && user.Id != msg.Author.Id && user.JoinedAt.HasValue)
                        info +=
                            $"Joined At: `{string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss zzzz}", user.JoinedAt.Value.LocalDateTime)}GMT`" +
                            $"(about {(DateTime.UtcNow - user.JoinedAt.Value).Days} days ago)\n";
                    if (dbUser.Warnings != null && !dbUser.Warnings.Empty())
                        info += $"`{dbUser.Warnings.Count}` Warnings: {dbUser.Warnings.SumAnd()}";

                    foreach (var str in info.SplitSafe(','))
                    {
                        await msg.ReplyAsync(str.TrimStart(','));
                    }
                }
            };

            ModCommands.Add(find);

            Command addwarning = new Command("addwarning");
            addwarning.Description += "Add a warning to the database";
            addwarning.Usage = "addwarning <user> <warning>";
            addwarning.RequiredPermission = Command.PermissionLevels.Moderator;
            addwarning.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync("You must specify a user");
                    return;
                }
                ulong uid;
                if (ulong.TryParse(parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out uid))
                {
                    parameters.RemoveAt(0);
                    string warning = parameters.reJoin();
                    warning += $" (Added By `{msg.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)";
                    DBGuild guildDb = new DBGuild(msg.GetGuild().Id);
                    guildDb.AddOrUpdateUser(guildDb.GetOrCreateUser(uid).AddWarning(warning));
                    var builder = new EmbedBuilder()
                        .WithTitle("Warning Added")
                        .WithDescription(warning)
                        .WithColor(new Color(0xFFFF00))
                        .WithFooter(footer =>
                        {
                            footer
                                .WithText($"By {msg.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                        });

                    var warnedDbUser = guildDb.Users.Find(u => u.ID.Equals(uid));

                    EmbedFieldBuilder warningCountField = new EmbedFieldBuilder().WithName("Warning Count").WithValue(warnedDbUser.Warnings.Count).WithIsInline(true);
                    builder.AddField(warningCountField);

                    try
                    {
                        var user = client.GetUser(uid);
                        builder.Author = new EmbedAuthorBuilder().WithName(user.ToString()).WithIconUrl(user.GetAvatarUrl());
                    }
                    catch
                    {
                        builder.Author = new EmbedAuthorBuilder().WithName(uid.ToString());
                    }


                    await msg.Channel.SendMessageAsync("", embed: builder.Build());
                    if (GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId != 0)
                    {
                        await ((SocketTextChannel)client.GetChannel(GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId))
                            .SendMessageAsync("", embed: builder.Build());
                    }
                }
                else
                {
                    await msg.ReplyAsync("Could not find that user");
                }

            };

            ModCommands.Add(addwarning);

            Command issuewarning = new Command("issuewarning");
            issuewarning.Description += "Add a warning to the database and send it to the user";
            issuewarning.Usage = "issuewarning <user> <warning>";
            issuewarning.RequiredPermission = Command.PermissionLevels.Moderator;
            issuewarning.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync("You must specify a user");
                    return;
                }
                if (msg.GetMentionedUsers().Any())
                {
                    var user = msg.GetMentionedUsers().First();
                    if (!msg.GetGuild().Users.Any(u => u.Id.Equals(user.Id)))
                    {
                        await msg.ReplyAsync("Could not find that user");
                        return;
                    }
                    parameters.RemoveAt(0);
                    string warning = parameters.reJoin();
                    warning += $" (Issued By `{msg.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)";

                    DBGuild guildDb = new DBGuild(msg.GetGuild().Id);
                    guildDb.AddOrUpdateUser(guildDb.GetOrCreateUser(user.Id).AddWarning(warning));

                    try
                    {
                        await user.GetOrCreateDMChannelAsync().Result.SendMessageAsync(
                            $"The Moderator team of **{msg.GetGuild().Name}** has issued you the following warning:\n{parameters.reJoin()}");
                    }
                    catch
                    {
                        warning += $"\nCould not message {user}";
                    }

                    var builder = new EmbedBuilder()
                        .WithTitle("Warning Issued")
                        .WithDescription(warning)
                        .WithColor(new Color(0xFFFF00))
                        .WithFooter(footer =>
                        {
                            footer
                                .WithText($"By {msg.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                        });

                    var warnedDbUser = guildDb.Users.Find(u => u.ID.Equals(user.Id));

                    EmbedFieldBuilder warningCountField = new EmbedFieldBuilder().WithName("Warning Count").WithValue(warnedDbUser.Warnings.Count).WithIsInline(true);
                    builder.AddField(warningCountField);

                    builder.Author = new EmbedAuthorBuilder().WithName(user.ToString()).WithIconUrl(user.GetAvatarUrl());

                    await msg.Channel.SendMessageAsync("", embed: builder.Build());
                    if (GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId != 0)
                    {
                        await ((SocketTextChannel)client.GetChannel(GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId))
                            .SendMessageAsync("", embed: builder.Build());
                    }
                }
                else
                {
                    await msg.ReplyAsync("Could not find that user");
                }

            };

            ModCommands.Add(issuewarning);

            Command removeWarning = new Command("removeWarning");
            removeWarning.RequiredPermission = Command.PermissionLevels.Moderator;
            removeWarning.Usage = "removewarning <user>";
            removeWarning.Description = "Remove the last warning from a user";
            removeWarning.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                bool removeAll = false;
                if (parameters[0].ToLower().Equals("all"))
                {
                    removeAll = true;
                    parameters.RemoveAt(0);
                }

                ulong uid;
                if (ulong.TryParse(parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out uid))
                {
                    var guilddb = new DBGuild(msg.GetGuild().Id);
                    try
                    {
                        guilddb.AddOrUpdateUser(guilddb.GetOrCreateUser(uid).RemoveWarning(allWarnings: removeAll));
                        await msg.ReplyAsync($"Done!");
                    }
                    catch (DivideByZeroException ex)
                    {
                        await msg.ReplyAsync("User had no warnings");
                    }
                }
                else await msg.ReplyAsync($"No user found");
            };

            ModCommands.Add(removeWarning);

            Command mute = new Command("mute");
            mute.RequiredPermission = Command.PermissionLevels.Moderator;
            mute.Usage = "mute <user>";
            mute.Description = "Mute a user";
            mute.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync($"You need to specify a user!");
                    return;
                }
                var gc = GenericBot.GuildConfigs[msg.GetGuild().Id];
                if (!msg.GetGuild().Roles.Any(r => r.Id == gc.MutedRoleId))
                {
                    await msg.ReplyAsync("The Muted Role Id is configured incorrectly. Please talk to your server admin");
                    return;
                }
                var mutedRole = msg.GetGuild().Roles.First(r => r.Id == gc.MutedRoleId);
                List<IUser> mutedUsers = new List<IUser>();
                foreach (var user in msg.GetMentionedUsers().Select(u => u.Id))
                {
                    try
                    {
                        await (msg.GetGuild().GetUser(user)).AddRolesAsync(new List<IRole> { mutedRole });
                        gc.ProbablyMutedUsers.Add(user);
                        gc.Save();
                        mutedUsers.Add(msg.GetGuild().GetUser(user));
                    }
                    catch
                    {
                    }
                }

                string res;

                if(mutedUsers.Count > 0) 
                {
                    res = "Succesfully muted " + mutedUsers.Select(u => u.Mention).ToList().SumAnd();
                }
                else
                {
                    res = "Could not find that user";
                }

                await msg.ReplyAsync(res);


            };

            ModCommands.Add(mute);

            Command unmute = new Command("unmute");
            unmute.RequiredPermission = Command.PermissionLevels.Moderator;
            unmute.Usage = "unmute <user>";
            unmute.Description = "Unmute a user";
            unmute.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync($"You need to specify a user!");
                    return;
                }
                var gc = GenericBot.GuildConfigs[msg.GetGuild().Id];
                if (!msg.GetGuild().Roles.Any(r => r.Id == gc.MutedRoleId))
                {
                    await msg.ReplyAsync("The Muted Role Id is configured incorrectly. Please talk to your server admin");
                    return;
                }
                var mutedRole = msg.GetGuild().Roles.First(r => r.Id == gc.MutedRoleId);
                List<IUser> mutedUsers = new List<IUser>();
                foreach (var user in msg.GetMentionedUsers().Select(u => u.Id))
                {
                    try
                    {
                        await (msg.GetGuild().GetUser(user)).RemoveRoleAsync(mutedRole);
                        gc.ProbablyMutedUsers.Remove(user);
                        mutedUsers.Add(msg.GetGuild().GetUser(user));
                    }
                    catch
                    {
                    }
                }
                gc.Save();

                string res = "Succesfully unmuted ";
                for (int i = 0; i < mutedUsers.Count; i++)
                {
                    if (i == mutedUsers.Count - 1 && mutedUsers.Count > 1)
                    {
                        res += $"and {mutedUsers.ElementAt(i).Mention}";
                    }
                    else
                    {
                        res += $"{mutedUsers.ElementAt(i).Mention}, ";
                    }
                }

                await msg.ReplyAsync(res.TrimEnd(',', ' '));
            };

            ModCommands.Add(unmute);

            return ModCommands;
        }
    }
}
