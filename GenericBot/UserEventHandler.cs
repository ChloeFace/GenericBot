using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GenericBot.Entities;

namespace GenericBot
{
    public static class UserEventHandler
    {
        public static Task UserUpdated(SocketGuildUser beforeUser, SocketGuildUser afterUser)
        {
            if (beforeUser.Username != afterUser.Username || beforeUser.Nickname != afterUser.Nickname)
            {
                var guildDb = new DBGuild(afterUser.Guild.Id);
                var user = guildDb.GetOrCreateUser(afterUser.Id);
                user.AddUsername(beforeUser.Username);
                user.AddNickname(beforeUser);
                user.AddUsername(afterUser.Username);
                user.AddNickname(afterUser);
                guildDb.AddOrUpdateUser(user);
            }
            return Task.CompletedTask;
        }

        public static async Task UserJoined(SocketGuildUser user)
        {
            #region Database

            var guildDb = new DBGuild(user.Guild.Id);
            bool alreadyJoined = false;
            if (guildDb.Users.Any(u => u.ID.Equals(user.Id))) // if already exists
            {
                alreadyJoined = true;
            }
            var dbUser = guildDb.GetOrCreateUser(user.Id);
            dbUser.AddUsername(user.Username);
            guildDb.AddOrUpdateUser(dbUser);
            
            #endregion Databasae

            #region Logging

            var guildConfig = GenericBot.GuildConfigs[user.Guild.Id];

            if (!(guildConfig.VerifiedRole == 0 || (string.IsNullOrEmpty(guildConfig.VerifiedMessage) || guildConfig.VerifiedMessage.Split().Length < 32 || !user.Guild.Roles.Any(r => r.Id == guildConfig.VerifiedRole))))
            {
                string vMessage = $"Hey {user.Username}! To get verified on **{user.Guild.Name}** reply to this message with the hidden code in the message below\n\n"
                                 + GenericBot.GuildConfigs[user.Guild.Id].VerifiedMessage;

                string verificationMessage =
                    VerificationEngine.InsertCodeInMessage(vMessage, VerificationEngine.GetVerificationCode(user.Id, user.Guild.Id));

                try
                {
                    await user.GetOrCreateDMChannelAsync().Result.SendMessageAsync(verificationMessage);
                }
                catch
                {
                    await GenericBot.Logger.LogErrorMessage($"Could not send verification DM to {user} ({user.Id}) on {user.Guild} ({user.Guild.Id})");
                }
            }
            if (guildConfig.ProbablyMutedUsers.Contains(user.Id))
            {
                try { await user.AddRoleAsync(user.Guild.GetRole(guildConfig.MutedRoleId)); }
                catch { }
            }
            if(guildConfig.AutoRoleIds != null && guildConfig.AutoRoleIds.Any())
            {
                foreach(var role in guildConfig.AutoRoleIds)
                {
                    try
                    {
                        await user.AddRoleAsync(user.Guild.GetRole(role));
                    }
                    catch
                    {
                        // Supress
                    }
                }
            }

            if (guildConfig.UserLogChannelId == 0) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder().WithName("User Joined")
                    .WithIconUrl(user.GetAvatarUrl()).WithUrl(user.GetAvatarUrl()))
                .WithColor(114, 137, 218)
                .AddField(new EmbedFieldBuilder().WithName("Username").WithValue(user.ToString().Escape()).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("UserId").WithValue(user.Id).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Mention").WithValue(user.Mention).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("User Number").WithValue(user.Guild.MemberCount + (!alreadyJoined ? " (New Member)" : "")).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Database Number").WithValue(guildDb.Users.Count + (alreadyJoined ? " (Previous Member)" : "")).WithIsInline(true))
                .WithFooter($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");

            if ((DateTimeOffset.Now - user.CreatedAt).TotalDays < 7)
            {
                log.AddField(new EmbedFieldBuilder().WithName("New User")
                    .WithValue($"Account made {(DateTimeOffset.Now - user.CreatedAt).Nice()} ago").WithIsInline(true));
            }

            try
            {
                DBUser usr = guildDb.Users.First(u => u.ID.Equals(user.Id));

                if (!usr.Warnings.Empty())
                {
                    string warns = "";
                    for (int i = 0; i < usr.Warnings.Count; i++)
                    {
                        warns += $"{i + 1}: {usr.Warnings.ElementAt(i)}\n";
                    }
                    log.AddField(new EmbedFieldBuilder().WithName($"{usr.Warnings.Count} Warnings")
                        .WithValue(warns));
                }
            }
            catch
            {
            }

            await user.Guild.GetTextChannel(guildConfig.UserLogChannelId).SendMessageAsync("", embed: log.Build());

            #endregion Logging

            #region Antispam

            if (guildConfig.AntispamLevel >= GuildConfig.AntiSpamLevel.Advanced)
            {
                var inviteLink = new Regex(@"(?:https?:\/\/)?(?:www\.)?(discord\.gg|discord\.io|discord\.me|discordapp\.com\/invite|paypal\.me|twitter\.com|youtube\.com|bit\.ly|twitch\.tv)\/(\S+)");
                if (inviteLink.Matches(user.Username).Any())
                {
                    if(guildConfig.AntispamLevel == GuildConfig.AntiSpamLevel.Advanced)
                    {
                        await user.KickAsync("Username Contains Discord Spam Invite");
                        var builder = new EmbedBuilder()
                        .WithTitle("User Kicked")
                        .WithDescription("Discord Invite in Username (Antispam)")
                        .WithColor(new Color(0xFFFF00))
                        .WithFooter(footer => {
                            footer
                                .WithText($"By {GenericBot.DiscordClient.CurrentUser} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                        })
                        .WithAuthor(author => {
                            author
                                .WithName(user.ToString())
                                .WithIconUrl(user.GetAvatarUrl());
                        });

                        var guilddb = new DBGuild(user.Guild.Id);
                        var guildconfig = GenericBot.GuildConfigs[user.Guild.Id];
                        var kickedUser = guilddb.GetOrCreateUser(user.Id)
                            .AddWarning(
                                $"Kicked for `Username Contains Discord Spam Invite` (By `{GenericBot.DiscordClient.CurrentUser}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                        guilddb.AddOrUpdateUser(kickedUser);

                        if (guildconfig.UserLogChannelId != 0)
                        {
                            await (GenericBot.DiscordClient.GetChannel(guildconfig.UserLogChannelId) as SocketTextChannel)
                                .SendMessageAsync("", embed: builder.Build());
                        }
                    }
                    else if(guildConfig.AntispamLevel >= GuildConfig.AntiSpamLevel.Aggressive)
                    {
                        await user.BanAsync(0, "Username Contains Discord Spam Invite");
                        var builder = new EmbedBuilder()
                        .WithTitle("User Banned")
                        .WithDescription("Discord Invite in Username (Antispam)")
                        .WithColor(new Color(0xFFFF00))
                        .WithFooter(footer => {
                            footer
                                .WithText($"By {GenericBot.DiscordClient.CurrentUser} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                        })
                        .WithAuthor(author => {
                            author
                                .WithName(user.ToString())
                                .WithIconUrl(user.GetAvatarUrl());
                        });

                        var guilddb = new DBGuild(user.Guild.Id);
                        var guildconfig = GenericBot.GuildConfigs[user.Guild.Id];
                        var bannedUser = guilddb.GetOrCreateUser(user.Id)
                            .AddWarning(
                                $"Banned for `Username Contains Discord Spam Invite` (By `{GenericBot.DiscordClient.CurrentUser}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                        guilddb.AddOrUpdateUser(bannedUser);

                        if (guildconfig.UserLogChannelId != 0)
                        {
                            await (GenericBot.DiscordClient.GetChannel(guildconfig.UserLogChannelId) as SocketTextChannel)
                                .SendMessageAsync("", embed: builder.Build());
                        }
                    }
                }
            }

            #endregion Antispam
        }

        public static async Task UserLeft(SocketGuildUser user)
        {
            #region Logging

            var guildConfig = GenericBot.GuildConfigs[user.Guild.Id];

            if (guildConfig.UserLogChannelId == 0) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder().WithName("User Left")
                    .WithIconUrl(user.GetAvatarUrl()).WithUrl(user.GetAvatarUrl()))
                .WithColor(156, 39, 176)
                .AddField(new EmbedFieldBuilder().WithName("Username").WithValue(user.ToString().Escape()).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("UserId").WithValue(user.Id).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Mention").WithValue(user.Mention).WithIsInline(true))
                .WithFooter($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");

            await user.Guild.GetTextChannel(guildConfig.UserLogChannelId).SendMessageAsync("", embed: log.Build());

            #endregion Logging
        }
    }
}
