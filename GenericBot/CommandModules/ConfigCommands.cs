using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    public class ConfigCommands
    {
        public List<Command> GetConfigComamnds()
        {
            List<Command> ConfigCommands = new List<Command>();

            Command config = new Command("config");
            config.Usage = "config <option> <value>`\nOptions are: `adminroles`, `moderatorroles`, `userroles`, `twitter`, `user`, `mutedroleid";
            config.Description = "Configure the bot's option";
            config.RequiredPermission = Command.PermissionLevels.Admin;
            config.ToExecute += async (client, msg, paramList) =>
            {
                if (paramList.Empty())
                {
                    await msg.Channel.SendMessageAsync($"Please enter a value to configure.");
                    return;
                }
                #region AdminRoles
                if (paramList[0].ToLower().Equals("adminroles"))
                {
                    if (config.GetPermissions(msg.Author, msg.GetGuild().Id) >= Command.PermissionLevels.GuildOwner)
                    {
                        if (paramList.Count == 1)
                        {
                            await msg.ReplyAsync($"Please enter a config option");
                            return;
                        }
                        if (paramList.Count == 2)
                        {
                            await msg.ReplyAsync($"Please enter a roleId");
                            return;
                        }
                        ulong id;
                        if (ulong.TryParse(paramList[2], out id) && msg.GetGuild().Roles.Select(r => r.Id).Any(u => u.Equals(id)))
                        {
                            if (paramList[1].ToLower().Equals("add"))
                            {
                                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].AdminRoleIds.Contains(id))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].AdminRoleIds.Add(id);
                                    await msg.ReplyAsync($"Added {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} to Admin Roles");
                                }
                                else { await msg.ReplyAsync($"Admin Roles already contains {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}"); }
                            }
                            else if (paramList[1].ToLower().Equals("remove"))
                            {
                                if (GenericBot.GuildConfigs[msg.GetGuild().Id].AdminRoleIds.Contains(id))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].AdminRoleIds.Remove(id);
                                    await msg.ReplyAsync($"Removed {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} from Admin Roles");
                                }
                                else
                                {
                                    await msg.ReplyAsync(
                                        $"Admin Roles doesn't contain {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}");
                                }
                            }
                            else { await msg.ReplyAsync($"Unknown property `{paramList[1]}`."); }
                        }
                        else { await msg.ReplyAsync($"That is not a valid roleId"); }
                    }
                    else { await msg.ReplyAsync($"You don't have the permissions to do that"); }
                }
                #endregion AdminRoles

                #region ModRoles

                else if (paramList[0].ToLower().Equals("moderatorroles") || paramList[0].ToLower().Equals("modroles"))
                {
                    if (config.GetPermissions(msg.Author, msg.GetGuild().Id) >= Command.PermissionLevels.Admin)
                    {
                        if (paramList.Count == 1)
                        {
                            await msg.ReplyAsync($"Please enter a config option");
                            return;
                        }
                        if (paramList.Count == 2)
                        {
                            await msg.ReplyAsync($"Please enter a roleId");
                            return;
                        }
                        ulong id;
                        if (ulong.TryParse(paramList[2], out id) && msg.GetGuild().Roles.Select(r => r.Id).Any(u => u.Equals(id)))
                        {
                            if (paramList[1].ToLower().Equals("add"))
                            {
                                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].ModRoleIds.Contains(id))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].ModRoleIds.Add(id);
                                    await msg.ReplyAsync($"Added {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} to Moderator Roles");
                                }
                                else
                                {
                                    await msg.ReplyAsync($"Moderator Roles already contains {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}");
                                }
                            }
                            else if (paramList[1].ToLower().Equals("remove"))
                            {
                                if (GenericBot.GuildConfigs[msg.GetGuild().Id].ModRoleIds.Contains(id))
                                {
                                    {
                                        GenericBot.GuildConfigs[msg.GetGuild().Id].ModRoleIds.Remove(id);
                                        await msg.ReplyAsync(
                                            $"Removed {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} from Moderator Roles");
                                    }
                                }
                                else
                                {
                                    await msg.ReplyAsync(
                                        $"Moderator Roles doesn't contain {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}");
                                }
                            }
                            else { await msg.ReplyAsync($"Unknown property `{paramList[1]}`."); }
                        }
                        else { await msg.ReplyAsync($"That is not a valid roleId"); }
                    }
                    else { await msg.ReplyAsync($"You don't have the permissions to do that"); }
                }

                #endregion ModRoles

                #region UserRoles

                else if (paramList[0].ToLower().Equals("userroles"))
                {
                    if (config.GetPermissions(msg.Author, msg.GetGuild().Id) >= Command.PermissionLevels.Admin)
                    {
                        if (paramList.Count == 1)
                        {
                            await msg.ReplyAsync($"Please enter a config option");
                            return;
                        }
                        if (paramList.Count == 2)
                        {
                            await msg.ReplyAsync($"Please enter a roleId");
                            return;
                        }
                        ulong id;
                        if (ulong.TryParse(paramList[2], out id) && msg.GetGuild().Roles.Select(r => r.Id).Any(u => u.Equals(id)))
                        {
                            if (paramList[1].ToLower().Equals("add"))
                            {
                                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].UserRoleIds.Contains(id))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].UserRoleIds.Add(id);
                                    await msg.ReplyAsync($"Added {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} to User Roles");
                                }
                                else
                                {
                                    await msg.ReplyAsync($"User Roles already contains {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}");
                                }
                            }
                            else if (paramList[1].ToLower().Equals("remove"))
                            {
                                if (GenericBot.GuildConfigs[msg.GetGuild().Id].UserRoleIds.Contains(id))
                                {
                                    {
                                        GenericBot.GuildConfigs[msg.GetGuild().Id].UserRoleIds.Remove(id);
                                        await msg.ReplyAsync(
                                            $"Removed {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} from User Roles");
                                    }
                                }
                                else { await msg.ReplyAsync($"User Roles doesn't contain {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}"); }
                            }
                            else { await msg.ReplyAsync($"Unknown property `{paramList[1]}`."); }
                        }
                        else { await msg.ReplyAsync($"That is not a valid roleId"); }
                    }
                    else { await msg.ReplyAsync($"You don't have the permissions to do that"); }
                }

                #endregion UserRoles

                #region Twitter

                else if (paramList[0].ToLower().Equals("twitter"))
                {
                    if (paramList[1].ToLower() == "true")
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].AllowTwitter = true;
                        await msg.ReplyAsync("Tweeting on this server has been enabled");
                    }
                    else if (paramList[1].ToLower() == "false")
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].AllowTwitter = false;
                        await msg.ReplyAsync("Tweeting on this server has been disabled");
                    }
                    else
                    {
                        await msg.ReplyAsync("That's not a valid option");
                    }
                }

                #endregion Twitter

                #region Prefix

                else if (paramList[0].ToLower().Equals("prefix"))
                {
                    try
                    {
                        paramList.RemoveAt(0);
                        GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix = paramList.reJoin();
                        if (msg.Content.EndsWith('"') && paramList[0].ToCharArray()[0].Equals('"'))
                        {
                            GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix = new Regex("\"(.*?)\"").Match(msg.Content).Value.Trim('"');
                        }
                        await msg.ReplyAsync($"The prefix has been set to `{GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix}`");
                    }
                    catch
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix = "";
                        await msg.ReplyAsync($"The prefix has been reset to the default of `{GenericBot.GlobalConfiguration.DefaultPrefix}`");
                    }
                }

                #endregion Prefix

                #region Logging

                else if (paramList[0].ToLower().Equals("logging"))
                {
                    if (paramList[1].ToLower().Equals("channelid"))
                    {
                        if (paramList.Count() == 2)
                        {
                            await msg.ReplyAsync(
                                $"Current user event channel: <#{GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId}>");
                        }
                        else
                        {
                            ulong cId;
                            if (ulong.TryParse(paramList[2], out cId) && (msg.GetGuild().Channels.Any(c => c.Id == cId) || cId == 0))
                            {
                                GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId = cId;
                                await msg.ReplyAsync(
                                    $"User event channel set to <#{GenericBot.GuildConfigs[msg.GetGuild().Id].UserLogChannelId}>");
                            }
                            else await msg.ReplyAsync("Invalid Channel Id");
                        }
                    }
                    else if (paramList[1].ToLower().Equals("ignorechannel"))
                    {
                        if (paramList.Count == 2)
                        {
                            string m = "Ignored channels:";
                            foreach (var id in GenericBot.GuildConfigs[msg.GetGuild().Id].MessageLoggingIgnoreChannels)
                            {
                                m += $"\n<#{id}>";
                            }

                            await msg.ReplyAsync(m);
                        }
                        else
                        {
                            if (ulong.TryParse(paramList[2], out ulong cId) &&
                                msg.GetGuild().TextChannels.Any(c => c.Id == cId))
                            {
                                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].MessageLoggingIgnoreChannels.Contains(cId))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].MessageLoggingIgnoreChannels.Add(cId);
                                    await msg.ReplyAsync($"No longer logging <#{cId}>");
                                }
                                else
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].MessageLoggingIgnoreChannels.Remove(cId);
                                    await msg.ReplyAsync($"Resuming logging <#{cId}>");
                                }
                            }
                            else
                            {
                                await msg.ReplyAsync("Invalid Channel Id");
                            }
                        }
                    }
                }

                #endregion Logging

                #region MutedRoleId

                else if (paramList[0].ToLower().Equals("mutedroleid"))
                {
                    paramList.RemoveAt(0);
                    if (paramList.Count != 1)
                    {
                        await msg.ReplyAsync(
                            "Incorrect number of arguments. Make sure the command is `voicerole [VoiceChannelId] [RoleId]`");
                        return;
                    }
                    else
                    {
                        ulong roleId;
                        if (ulong.TryParse(paramList[0], out roleId) && (msg.GetGuild().Roles.Any(r => r.Id == roleId) || roleId == 0))
                        {
                            GenericBot.GuildConfigs[msg.GetGuild().Id].MutedRoleId = roleId;
                            GenericBot.GuildConfigs[msg.GetGuild().Id].Save();
                            await msg.ReplyAsync($"MutedRoleId is now `{roleId}`");
                        }
                        else
                        {
                            await msg.ReplyAsync("Invalid Role Id");
                        }
                    }
                }

                #endregion MutedRoleId

                #region Verification

                else if (paramList[0].ToLower().Equals("verification"))
                {
                    if (paramList[1].ToLower().Equals("roleid"))
                    {
                        if (paramList.Count == 2)
                        {
                            var roleId = GenericBot.GuildConfigs[msg.GetGuild().Id].VerifiedRole;
                            if (roleId == 0)
                            {
                                await msg.ReplyAsync("Verification is disabled on this server");
                            }
                            else
                            {
                                await msg.ReplyAsync(
                                    $"Verification role is  `{msg.GetGuild().Roles.First(g => g.Id == roleId).Name}`");
                            }
                        }
                        else if (ulong.TryParse(paramList[2], out ulong roleId) && (msg.GetGuild().Roles.Any(g => g.Id == roleId) || roleId == 0))
                        {
                            GenericBot.GuildConfigs[msg.GetGuild().Id].VerifiedRole = roleId;
                            if (roleId != 0)
                            {
                                await msg.ReplyAsync(
                                    $"Verification role set to `{msg.GetGuild().Roles.First(g => g.Id == roleId).Name}`");
                            }
                            else
                            {
                                await msg.ReplyAsync("Verification role cleared. Verification is off for this server.");
                            }
                        }
                        else
                        {
                            await msg.ReplyAsync("Invalid RoleId");
                        }
                    }
                    else if (paramList[1].ToLower().Equals("message"))
                    {
                        string pref = GenericBot.GlobalConfiguration.DefaultPrefix;
                        if (!String.IsNullOrEmpty(GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix))
                            pref = GenericBot.GuildConfigs[msg.GetGuild().Id].Prefix;

                        string message = msg.Content;
                        message = message.Remove(0, pref.Length).TrimStart(' ').Remove(0, "config".Length).TrimStart(' ').Remove(0, "verification".Length).TrimStart(' ').Remove(0, "message".Length).Trim(' ');

                        if (!string.IsNullOrEmpty(message)) GenericBot.GuildConfigs[msg.GetGuild().Id].VerifiedMessage = message;

                        await msg.ReplyAsync("Example verification message:");

                        string vm = $"Hey {msg.Author.Username}! To get verified on **{msg.GetGuild().Name}** reply to this message with the hidden code in the message below\n\n"
                                         + GenericBot.GuildConfigs[msg.GetGuild().Id].VerifiedMessage;

                        string verificationMessage =
                            VerificationEngine.InsertCodeInMessage(vm, VerificationEngine.GetVerificationCode(msg.Author.Id, msg.GetGuild().Id));

                        await msg.ReplyAsync(verificationMessage);
                    }
                    else await msg.ReplyAsync("Invalid Option");
                }

                #endregion Verification

                #region Points

                else if (paramList[0].ToLower().Equals("points"))
                {
                    if (paramList[1].ToLower().Equals("enabled"))
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].PointsEnabled =
                            !GenericBot.GuildConfigs[msg.GetGuild().Id].PointsEnabled;
                        if (GenericBot.GuildConfigs[msg.GetGuild().Id].PointsEnabled)
                        {
                            await msg.ReplyAsync($"Enabled points for this server");
                        }
                        else
                        {
                            await msg.ReplyAsync($"Disabled points for this server");
                        }
                    }
                    else if (paramList[1].ToLower().Equals("noun"))
                    {
                        if (paramList.Count != 3)
                        {
                            await msg.ReplyAsync("Please use one word as the name for the points name");
                        }
                        else
                        {
                            GenericBot.GuildConfigs[msg.GetGuild().Id].PointsName = paramList[2];
                            await msg.ReplyAsync($"Set the name for points to `{paramList[2]}`");
                        }
                    }
                    else if (paramList[1].ToLower().Equals("verb"))
                    {
                        if (paramList.Count != 3)
                        {
                            await msg.ReplyAsync("Please use one word as the name for the points verb");
                            return;
                        }
                        else
                        {
                            GenericBot.GuildConfigs[msg.GetGuild().Id].PointsVerb = paramList[2];
                            await msg.ReplyAsync($"Set the verb for using points to `{paramList[2]}`");
                        }
                    }
                    else
                    {
                        await msg.ReplyAsync("Unknown option");
                    }
                }

                #endregion Points

                #region GlobalBanOptOut

                else if (paramList[0].ToLower().Equals("globalbanoptout"))
                {
                    if (paramList.Count > 1 && paramList[1].ToLower().Equals("true"))
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].GlobalBanOptOut = true;
                        await msg.ReplyAsync($"You have opted out of the global bans");
                    }
                    else if (paramList.Count > 1 && paramList[1].ToLower().Equals("false"))
                    {

                        GenericBot.GuildConfigs[msg.GetGuild().Id].GlobalBanOptOut = false;
                        await msg.ReplyAsync($"You have opted into the global bans");
                    }
                    else
                    {
                        GenericBot.GuildConfigs[msg.GetGuild().Id].GlobalBanOptOut = !GenericBot.GuildConfigs[msg.GetGuild().Id].GlobalBanOptOut;
                        if (GenericBot.GuildConfigs[msg.GetGuild().Id].GlobalBanOptOut)
                        {
                            await msg.ReplyAsync($"You have opted out of the global bans");
                        }
                        else
                        {
                            await msg.ReplyAsync($"You have opted into the global bans");
                        }
                    }
                }

                #endregion GlobalBanOptOut

                #region AutoRole

                else if (paramList[0].ToLower().Equals("autoroles") || paramList[0].ToLower().Equals("autorole"))
                {
                    if (paramList.Count == 2)
                    {
                        await msg.ReplyAsync($"Please enter a roleId");
                        return;
                    }
                    if (ulong.TryParse(paramList[2], out ulong id))
                    {
                        if (paramList[1].ToLower().Equals("add"))
                        {
                            if (msg.GetGuild().Roles.Select(r => r.Id).Any(u => u.Equals(id)))
                            {
                                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].AutoRoleIds.Contains(id))
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].AutoRoleIds.Add(id);
                                    await msg.ReplyAsync($"Added {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name} to autoroles");
                                }
                                else
                                {
                                    await msg.ReplyAsync($"Autoroles already contains {msg.GetGuild().Roles.FirstOrDefault(r => r.Id == id).Name}");
                                }
                            }
                            else
                            {
                                await msg.ReplyAsync("Invalid RoleId (Not a role)");
                            }
                        }
                        else if (paramList[1].ToLower().Equals("remove"))
                        {
                            if (GenericBot.GuildConfigs[msg.GetGuild().Id].AutoRoleIds.Contains(id))
                            {
                                {
                                    GenericBot.GuildConfigs[msg.GetGuild().Id].AutoRoleIds.Remove(id);
                                    await msg.ReplyAsync(
                                        $"Removed `{id}` from autoroles");
                                }
                            }
                            else
                            {
                                await msg.ReplyAsync(
                                    $"The autoroles don't contain `{id}`");
                            }
                        }
                        else { await msg.ReplyAsync($"Unknown property `{paramList[1]}`."); }
                    }
                    else { await msg.ReplyAsync($"That is not a valid roleId"); }
                }

                #endregion AutoRole

                #region Antispam

                else if (paramList[0].ToLower().Equals("antispam"))
                {
                    if(paramList.Count != 2)
                    {
                        await msg.ReplyAsync("Please a single-word option");
                    }
                    else
                    {
                        switch (paramList[1].ToLower())
                        {
                            case "none":
                                GenericBot.GuildConfigs[msg.GetGuild().Id].AntispamLevel = GuildConfig.AntiSpamLevel.None;
                                await msg.ReplyAsync("Antispam Level **None** has been selected! The following options are enabled: None");
                                break;
                            case "basic":
                                GenericBot.GuildConfigs[msg.GetGuild().Id].AntispamLevel = GuildConfig.AntiSpamLevel.Basic;
                                await msg.ReplyAsync("Antispam Level **Basic** has been selected! The following options are enabled: None (yet)");
                                break;
                            case "advanced":
                                GenericBot.GuildConfigs[msg.GetGuild().Id].AntispamLevel = GuildConfig.AntiSpamLevel.Advanced;
                                await msg.ReplyAsync("Antispam Level **Advanced** has been selected! The following options are enabled: Username Link Kicking");
                                break;
                            case "aggressive":
                                GenericBot.GuildConfigs[msg.GetGuild().Id].AntispamLevel = GuildConfig.AntiSpamLevel.Aggressive;
                                await msg.ReplyAsync("Antispam Level **Aggressive** has been selected! The following options are enabled: Username Link Banning");
                                break;
                            case "activeraid":
                                GenericBot.GuildConfigs[msg.GetGuild().Id].AntispamLevel = GuildConfig.AntiSpamLevel.ActiveRaid;
                                await msg.ReplyAsync("Antispam Level **ActiveRaid** has been selected! The following options are enabled: Username Link Banning");
                                break;
                            default:
                                await msg.ReplyAsync("That is not an available option. You can select: `None`, `Basic`, `Advanced`, `Aggressive`, and `ActiveRaid`");
                                break;
                        }
                    }
                }

                #endregion Antispam

                else await msg.ReplyAsync($"Unknown property `{paramList[0]}`.");

                GenericBot.GuildConfigs[msg.GetGuild().Id].Save();
            };

            ConfigCommands.Add(config);

            Command levels = new Command(nameof(levels));
            levels.RequiredPermission = Command.PermissionLevels.Admin;
            levels.Description = "Set the number of points to get a role";
            levels.ToExecute += async (client, msg, parameters) =>
            {
                var guildConfig = GenericBot.GuildConfigs[msg.GetGuild().Id];
                if (parameters.Empty())
                {
                    if (guildConfig.Levels.Any(kvp => msg.GetGuild().GetRole(kvp.Value) != null))
                    {
                        string res = "";
                        foreach (var level in guildConfig.Levels.OrderBy(kvp => kvp.Key).Where(kvp => msg.GetGuild().GetRole(kvp.Value) != null))
                        {
                            var role = msg.GetGuild().GetRole(level.Value);
                            res += $"Role `{role.Name.Escape()}` at `{level.Key}` Points\n";
                        }
                        await msg.ReplyAsync(res);
                    }
                    else
                    {
                        await msg.ReplyAsync("There are no levels for this server!");
                    }
                }
                else
                {
                    if (parameters[0].ToLower() == "add")
                    {
                        if (parameters.Count == 3)
                        {
                            if (decimal.TryParse(parameters[1], out decimal pointValue) && ulong.TryParse(parameters[2], out ulong roleId)
                                && msg.GetGuild().Roles.Any(r => r.Id == roleId))
                            {
                                var db = new DBGuild(msg.GetGuild().Id);
                                guildConfig.Levels.Add(pointValue, roleId);
                                int addedUsers = 0;
                                foreach (var user in msg.GetGuild().Users)
                                {
                                    if (db.GetOrCreateUser(user.Id).PointsCount >= pointValue)
                                    {
                                        try
                                        {
                                            await user.AddRoleAsync(msg.GetGuild().GetRole(roleId));
                                            addedUsers++;
                                        }
                                        catch
                                        { }
                                    }
                                }
                                await msg.ReplyAsync($"Users will get `{msg.GetGuild().GetRole(roleId).Name.Escape()}` at `{pointValue}` points. `{addedUsers}` had the more than the number of points and have had the role assigned");
                            }
                        }
                        else
                        {
                            await msg.ReplyAsync($"The command should be formatted as `levels add pointsValue roleId`");
                        }
                    }
                    else if (parameters[0].ToLower() == "remove")
                    {
                        if (parameters.Count == 2)
                        {
                            if (ulong.TryParse(parameters[1], out ulong roleId) && guildConfig.Levels.Any(kvp => kvp.Value.Equals(roleId)))
                            {
                                guildConfig.Levels.Remove(guildConfig.Levels.First(kvp => kvp.Value.Equals(roleId)).Key);
                                await msg.ReplyAsync("Done!");
                            }
                            else
                            {
                                await msg.ReplyAsync("That is not a valid RoleId!");
                            }
                        }
                        else
                        {
                            await msg.ReplyAsync($"The command should be formatted as `levels remove roleId`");
                        }
                    }
                    guildConfig.Save();
                }
            };

            ConfigCommands.Add(levels);

            return ConfigCommands;
        }
    }
}
