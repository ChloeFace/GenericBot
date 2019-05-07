using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GenericBot.Entities;
using Newtonsoft.Json;

namespace GenericBot
{
    public static class MessageEventHandler
    {
        public static async Task MessageRecieved(SocketMessage parameterMessage, bool edited = false)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage;

            // Don't do stuff if the user is blacklisted
            if (GenericBot.GlobalConfiguration.BlacklistedIds.Contains(message.Author.Id))
                return;
            // Don't do stuff if the user is the bot
            if (parameterMessage.Author.Id == GenericBot.DiscordClient.CurrentUser.Id)
                return;

            if (parameterMessage.Channel.GetType().FullName.ToLower().Contains("dmchannel"))
            {
                var msg = GenericBot.DiscordClient.GetApplicationInfoAsync().Result.Owner.GetOrCreateDMChannelAsync().Result
                    .SendMessageAsync($"```\nDM from: {message.Author}({message.Author.Id})\nContent: {message.Content}\n```").Result;
                if (parameterMessage.Content.Trim().Split().Length == 1)
                {
                    var guild = VerificationEngine.GetGuildFromCode(parameterMessage.Content, message.Author.Id);
                    if (guild == null)
                    {
                        await message.ReplyAsync("Invalid verification code");
                    }
                    else
                    {
                        await guild.GetUser(message.Author.Id)
                            .AddRoleAsync(guild.GetRole(GenericBot.GuildConfigs[guild.Id].VerifiedRole));
                        if (guild.TextChannels.HasElement(c => c.Id == (GenericBot.GuildConfigs[guild.Id].UserLogChannelId), out SocketTextChannel logChannel))
                        {
                            await logChannel.SendMessageAsync($"`{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")}`:  `{message.Author}` (`{message.Author.Id}`) just verified");
                        }
                        await message.ReplyAsync($"You've been verified on **{guild.Name}**!");
                        await msg.ModifyAsync(m =>
                            m.Content = $"```\nDM from: {message.Author}({message.Author.Id})\nContent: {message.Content.SafeSubstring(1900)}\nVerified on {guild.Name}\n```");
                    }
                }
            }

            try
            {
                var guildDb = new DBGuild(message.GetGuild().Id);
                var dbUser = guildDb.GetOrCreateUser(message.Author.Id);
                dbUser.AddNickname(message.GetGuild().GetUser(message.Author.Id));
                dbUser.AddUsername(message.Author.Username);
                guildDb.AddOrUpdateUser(dbUser);
            }
            catch(Exception ex){ await GenericBot.Logger.LogErrorMessage(ex.Message + "\n" + ex.StackTrace); }
            if (!edited)
            {
                try
                {
                    new GuildMessageStats(parameterMessage.GetGuild().Id).AddMessage(parameterMessage.Author.Id).Save();
                }
                catch (Exception ex) { GenericBot.Logger.LogErrorMessage(ex.Message + "\n" + ex.StackTrace); }
            }

            if (parameterMessage.Author.Id != GenericBot.DiscordClient.CurrentUser.Id &&
                GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].FourChannelId == parameterMessage.Channel.Id)
            {

                await parameterMessage.DeleteAsync();
                await parameterMessage.ReplyAsync(
                    $"**[Anonymous]** {string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss}", DateTimeOffset.UtcNow)}\n{parameterMessage.Content}");
            }

            try
            {
                GenericBot.QuickWatch.Restart();

                var commandInfo = CommandHandler.ParseMessage(parameterMessage);

                CustomCommand custom = new CustomCommand();

                if (parameterMessage.Channel is IDMChannel) goto DMChannel;

                if (GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].CustomCommands
                        .HasElement(c => c.Name == commandInfo.Name, out custom) ||
                    GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].CustomCommands
                        .HasElement(c => c.Aliases.Any(a => a.Equals(commandInfo.Name)), out custom))
                {
                    if (custom.Delete)
                    {
                        await parameterMessage.DeleteAsync();
                    }
                    await parameterMessage.ReplyAsync(custom.Response);
                    new GuildMessageStats(parameterMessage.GetGuild().Id).AddCommand(parameterMessage.Author.Id, custom.Name).Save();
                }

            DMChannel:
                GenericBot.LastCommand = commandInfo;
                await GenericBot.Logger.LogGenericMessage($"Guild: {parameterMessage.GetGuild().Name} ({parameterMessage.GetGuild().Id}) Channel: {parameterMessage.Channel.Name} ({parameterMessage.Channel.Id}) User: {parameterMessage.Author} ({parameterMessage.Author.Id}) Command: {commandInfo.Command.Name} Parameters {JsonConvert.SerializeObject(commandInfo.Parameters)}");
                await commandInfo.Command.ExecuteCommand(GenericBot.DiscordClient, message, commandInfo.Parameters);
                new GuildMessageStats(parameterMessage.GetGuild().Id).AddCommand(parameterMessage.Author.Id, commandInfo.Command.Name).Save();
                //GenericBot.CommandCounter++;
            }
            catch (NullReferenceException nullRefEx)
            {
                //Console.WriteLine($"Probably ignore nullref: \n{nullRefEx.StackTrace}"); 
            }
            catch (Exception ex)
            {
                if (parameterMessage.Author.Id == GenericBot.GlobalConfiguration.OwnerId)
                {
                    await parameterMessage.ReplyAsync("```\n" + $"{ex.Message}\n{ex.StackTrace}".SafeSubstring(1000) +
                                                      "\n```");
                }
                await GenericBot.Logger.LogErrorMessage(ex.Message);
                Console.WriteLine($"{ex.StackTrace}");
            }

            // Run the point thread
            try
            {
                lock (message.GetGuild().Id.ToString())
                {
                    DBGuild db = new DBGuild(message.GetGuild().Id);
                    if (db.Users == null) return;
                    var dbUser = db.GetOrCreateUser(message.Author.Id);
                    if (!edited)
                    {
                        db.AddOrUpdateUser(dbUser.AddPoints((decimal)0.01));
                        if (message.Author.Id == 189378507724292096 && GenericBot.annoy2B && (db.GetOrCreateUser(189378507724292096).PointsCount*100) % 25 == 0)
                        {
                            message.ReplyAsync($"2b sleep");
                        }
                        if (GenericBot.GuildConfigs[message.GetGuild().Id].Levels.Any(kvp => kvp.Key <= dbUser.PointsCount))
                        {
                            foreach (var level in GenericBot.GuildConfigs[message.GetGuild().Id].Levels
                            .Where(kvp => kvp.Key <= dbUser.PointsCount)
                            .Where(kvp => !(message.Author as SocketGuildUser).Roles.Any(r => r.Id.Equals(kvp.Value))))
                            {
                                (message.Author as SocketGuildUser).AddRoleAsync(message.GetGuild().GetRole(level.Value));
                            }
                        }
                    }
                    var thanksRegex = new Regex(@"(\b)((thanks?)|(thx)|(ty))(\b)", RegexOptions.IgnoreCase);
                    if (thanksRegex.IsMatch(message.Content) && GenericBot.GuildConfigs[message.GetGuild().Id].PointsEnabled && message.MentionedUsers.Any())
                    {
                        if (dbUser.LastThanks.AddMinutes(1) < DateTimeOffset.UtcNow)
                        {
                            List<IUser> givenUsers = new List<IUser>();
                            foreach (var user in message.MentionedUsers)
                            {
                                if (user.Id == message.Author.Id) continue;

                                db.AddOrUpdateUser(db.GetOrCreateUser(user.Id).AddPoints((decimal)1));
                                givenUsers.Add(user);
                            }
                            if (givenUsers.Any())
                            {
                                message.ReplyAsync($"{givenUsers.Select(us => $"**{(us as SocketGuildUser).GetDisplayName()}**").ToList().SumAnd()} received a {GenericBot.GuildConfigs[message.GetGuild().Id].PointsName} of thanks from **{(message.Author as SocketGuildUser).GetDisplayName()}**");
                                dbUser.LastThanks = DateTimeOffset.UtcNow;
                                db.AddOrUpdateUser(dbUser);
                            }
                            else message.ReplyAsync("You can't give yourself a point!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GenericBot.Logger.LogErrorMessage($"{ex.Message}\nGuild:{message.GetGuild().Name} | {message.GetGuild().Id}\nChannel:{message.Channel.Name} | {message.Channel.Id}\nUser:{message.Author} | {message.Author.Id}\n{message.Content}");
            }
        }

        public static async Task HandleEditedCommand(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            if (arg1.Value.Content == arg2.Content) return;

            if (GenericBot.GlobalConfiguration.DefaultExecuteEdits)
            {
                await MessageEventHandler.MessageRecieved(arg2, edited: true);
            }

            var guildConfig = GenericBot.GuildConfigs[arg2.GetGuild().Id];

            if (guildConfig.UserLogChannelId == 0 || guildConfig.MessageLoggingIgnoreChannels.Contains(arg2.Channel.Id)
                                                  || !arg1.HasValue) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithTitle("Message Edited")
                .WithColor(243, 110, 33)
                .WithCurrentTimestamp();

            if (string.IsNullOrEmpty(arg2.Author.GetAvatarUrl()))
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg2.Author} ({arg2.Author.Id})"));
            }
            else
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg2.Author} ({arg2.Author.Id})")
                    .WithIconUrl(arg2.Author.GetAvatarUrl() + " "));
            }

            log.AddField(new EmbedFieldBuilder().WithName("Channel").WithValue("#" + arg2.Channel.Name).WithIsInline(true));
            log.AddField(new EmbedFieldBuilder().WithName("Sent At").WithValue(arg1.Value.Timestamp.ToString(@"yyyy-MM-dd HH:mm.ss") + "GMT").WithIsInline(true));

            log.AddField(new EmbedFieldBuilder().WithName("Before").WithValue(arg1.Value.Content.SafeSubstring(1016)));
            log.AddField(new EmbedFieldBuilder().WithName("After").WithValue(arg2.Content.SafeSubstring(1016)));

            await arg2.GetGuild().GetTextChannel(guildConfig.UserLogChannelId).SendMessageAsync("", embed: log.Build());
        }

        public static async Task MessageDeleted(Cacheable<IMessage, ulong> arg, ISocketMessageChannel channel)
        {
            if (!arg.HasValue) return;
            if (GenericBot.ClearedMessageIds.Contains(arg.Id)) return;
            var guildConfig = GenericBot.GuildConfigs[(arg.Value as SocketMessage).GetGuild().Id];

            if (guildConfig.UserLogChannelId == 0 || guildConfig.MessageLoggingIgnoreChannels.Contains(channel.Id)) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithTitle("Message Deleted")
                .WithColor(139, 0, 0)
                .WithCurrentTimestamp();

            if (string.IsNullOrEmpty(arg.Value.Author.GetAvatarUrl()))
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg.Value.Author} ({arg.Value.Author.Id})"));
            }
            else
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg.Value.Author} ({arg.Value.Author.Id})")
                    .WithIconUrl(arg.Value.Author.GetAvatarUrl() + " "));
            }

            log.AddField(new EmbedFieldBuilder().WithName("Channel").WithValue("#" + arg.Value.Channel.Name).WithIsInline(true));
            log.AddField(new EmbedFieldBuilder().WithName("Sent At").WithValue(arg.Value.Timestamp.ToString(@"yyyy-MM-dd HH:mm.ss") + "GMT").WithIsInline(true));


            if (!string.IsNullOrEmpty(arg.Value.Content))
            {
                log.WithDescription("**Message:** " + arg.Value.Content);
            }

            if (arg.Value.Attachments.Any())
            {
                log.AddField(new EmbedFieldBuilder().WithName("Attachments").WithValue(arg.Value.Attachments.Select(a =>
                    $"File: {a.Filename}").Aggregate((a, b) => a + "\n" + b)));
                log.WithImageUrl(arg.Value.Attachments.First().ProxyUrl);
            }

            if(string.IsNullOrEmpty(arg.Value.Content) && !arg.Value.Attachments.Any() && arg.Value.Embeds.Any())
            {
                log.WithDescription("**Embed:**\n```json\n" + JsonConvert.SerializeObject(arg.Value.Embeds.First(), Formatting.Indented) + "\n```");
            }

            log.Footer = new EmbedFooterBuilder().WithText(arg.Value.Id.ToString());

            await (arg.Value as SocketMessage).GetGuild().GetTextChannel(guildConfig.UserLogChannelId).SendMessageAsync("", embed: log.Build());
        }

        public static async Task MessageRecieved(SocketMessage arg)
        {
            await MessageEventHandler.MessageRecieved(arg, false);
        }
    }
}
