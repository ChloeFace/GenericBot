using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GenericBot.Entities
{
    class GuildMessageStats
    {
        public class StatsUser
        {
            public ulong Id { get; set; }
            public List<StatsYear> Years { get; set; }

            public StatsUser(ulong userId, int year, int month, int day)
            {
                this.Id = userId;
                this.Years = new List<StatsYear> { new StatsYear(year, month, day) };
            }
            public StatsUser() { }
        }
        public class StatsYear
        {
            public int Year { get; set; }
            public List<StatsMonth> Months { get; set; }

            public StatsYear(int year, int month, int day)
            {
                this.Year = year;
                this.Months = new List<StatsMonth> { new StatsMonth(month, day) };
            }
            public StatsYear() { }
        }
        public class StatsMonth
        {
            public int Month { get; set; }
            public List<StatsDay> Days { get; set; }

            public StatsMonth(int month, int day)
            {
                this.Month = month;
                this.Days = new List<StatsDay> { new StatsDay(day) };
            }
            public StatsMonth() { }
        }
        public class StatsDay
        {
            public int Day { get; set; }
            public int MessageCount { get; set; }
            public Dictionary<string, int> Commands { get; set; }

            public StatsDay(int day)
            {
                this.Day = day;
                MessageCount = 1;
            }
            public StatsDay() { }
        }

        public List<StatsUser> Users { get; set; }
        [BsonId] public ulong ID { get; set; }

        public GuildMessageStats()
        {
            Users = new List<StatsUser>();
        }
        public GuildMessageStats(ulong guildId)
        {
            this.ID = guildId;
            var guildDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var collection = guildDb.GetCollection<StatsUser>("guildMessageStats");

            this.Users = collection.AsQueryable().ToList();
        }

        public void Save()
        {
            var guildDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var collection = guildDb.GetCollection<StatsUser>("guildMessageStats");
            collection.DeleteMany(new BsonDocument());
            collection.InsertMany(this.Users);
        }


        public GuildMessageStats AddMessage(ulong uId)
        {
            var now = DateTimeOffset.UtcNow;
            if (Users.HasElement(u => u.Id.Equals(uId), out var user))
            {
                if (user.Years.HasElement(y => y.Year.Equals(now.Year), out var year))
                {
                    if (year.Months.HasElement(m => m.Month.Equals(now.Month), out var month))
                    {
                        if (month.Days.HasElement(d => d.Day.Equals(now.Day), out var day))
                        {
                            day.MessageCount++;
                        }
                        else
                        {
                            month.Days.Add(new StatsDay(now.Day));
                        }
                    }
                    else
                    {
                        year.Months.Add(new StatsMonth(now.Month, now.Day));
                    }
                }
                else
                {
                    user.Years.Add(new StatsYear(now.Year, now.Month, now.Day));
                }
            }
            else
            {
                this.Users.Add(new StatsUser(uId, now.Year, now.Month, now.Day));
            }
            return this;
        }

        public GuildMessageStats AddCommand(ulong uId, string command)
        {
            command = command.ToLower();
            var now = DateTimeOffset.UtcNow;
            if (Users.HasElement(u => u.Id.Equals(uId), out var user))
            {
                if (user.Years.HasElement(y => y.Year.Equals(now.Year), out var year))
                {
                    if (year.Months.HasElement(m => m.Month.Equals(now.Month), out var month))
                    {
                        if (month.Days.HasElement(d => d.Day.Equals(now.Day), out var day))
                        {
                            if(day.Commands == null)
                            {
                                day.Commands = new Dictionary<string, int>();
                            }
                            if (day.Commands.Any(k => k.Key.Equals(command)))
                            {
                                day.Commands[command]++;
                            }
                            else
                            {
                                day.Commands.Add(command, 1);
                            }
                        }
                        else
                        {
                            month.Days.Add(new StatsDay(now.Day));
                            var _day = month.Days.First(d => d.Day == now.Day);
                            if (_day.Commands.Any(k => k.Key.Equals(command)))
                            {
                                _day.Commands[command]++;
                            }
                            else
                            {
                                _day.Commands.Add(command, 1);
                            }
                        }
                    }
                    else
                    {
                        year.Months.Add(new StatsMonth(now.Month, now.Day));
                        var day = year.Months.First(m => m.Month == now.Month)
                            .Days.First(d => d.Day == now.Day);
                        if (day.Commands.Any(k => k.Key.Equals(command)))
                        {
                            day.Commands[command]++;
                        }
                        else
                        {
                            day.Commands.Add(command, 1);
                        }
                    }
                }
                else
                {
                    user.Years.Add(new StatsYear(now.Year, now.Month, now.Day));
                    var day = user.Years.First(y => y.Year == now.Year)
                        .Months.First(m => m.Month == now.Month)
                        .Days.First(d => d.Day == now.Day);
                    if (day.Commands.Any(k => k.Key.Equals(command)))
                    {
                        day.Commands[command]++;
                    }
                    else
                    {
                        day.Commands.Add(command, 1);
                    }
                }
            }
            else
            {
                this.Users.Add(new StatsUser(uId, now.Year, now.Month, now.Day));
                var day = this.Users.First(u => u.Id == uId)
                    .Years.First(y => y.Year == now.Year)
                    .Months.First(m => m.Month == now.Month)
                    .Days.First(d => d.Day == now.Day);
                if (day.Commands.Any(k => k.Key.Equals(command)))
                {
                    day.Commands[command]++;
                }
                else
                {
                    day.Commands.Add(command, 1);
                }
            }
            return this;
        }
    }

    class AnalyticsCommandLoader
    {
        public List<Command> GetAnalyticsCommand()
        {
            List<Command> cmds = new List<Command>();

            Command analytics = new Command("analytics");
            analytics.RequiredPermission = Command.PermissionLevels.GlobalAdmin;
            analytics.Description = "Get a ton of analytics information from the server";
            analytics.ToExecute += async (client, msg, parameters) =>
            {
                var stats = new GuildMessageStats(msg.GetGuild().Id);
                var users = stats.Users.Where(u => u.Id != 0);
                var years = users.SelectMany(u => u.Years);
                var months = years.SelectMany(y => y.Months);
                var days = months.SelectMany(m => m.Days);
                var commands = days.Where(d => d.Commands != null).SelectMany(d => d.Commands)
                    .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum()))
                    .ToList();

                var mostActiveIdOverall = users.OrderByDescending(u => u.Years.Sum(y => y.Months.Sum(m => m.Days.Sum(d => d.MessageCount)))).Take(3);
                string mostActiveUsersOverall = "";
                foreach (var user in mostActiveIdOverall)
                {
                    var commandCount = user.Years.Any(y => y.Months.Any(m => m.Days.Any(d => d.Commands != null))) ? user.Years.Sum(y => y.Months.Sum(m => m.Days.Where(d => d.Commands != null).Sum(d => d.Commands.Sum(c => c.Value)))) : 0;
                    if (msg.GetGuild().Users.HasElement(u => u.Id == user.Id, out var us))
                    {
                        mostActiveUsersOverall += $"    {us.GetDisplayName()} (`{us}`) " +
                        $"(`{user.Years.Sum(y => y.Months.Sum(m => m.Days.Sum(d => d.MessageCount)))}` messages, " +
                        $"`{commandCount}` commands)\n";
                    }
                    else
                    {
                        mostActiveUsersOverall += $"    Unknown User (`{user.Id}`) " +
                        $"(`{user.Years.Sum(y => y.Months.Sum(m => m.Days.Sum(d => d.MessageCount)))}` messages, " +
                        $"`{commandCount}` commands)\n";
                    }
                }

                var MostUsedCommandInfoOverall = commands.OrderByDescending(c => c.Value).Take(3);
                string MostUsedCommandsOverall = "";
                foreach (var cmd in MostUsedCommandInfoOverall)
                {
                    MostUsedCommandsOverall += $"    {cmd.Key} (`{cmd.Value}` uses)\n";
                }

                string mostActiveUsersToday = "";
                var mostActiveIdToday = GetTopToday(users);
                foreach (var user in mostActiveIdToday)
                {
                    if (msg.GetGuild().Users.HasElement(u => u.Id == user.Id, out var us))
                    {
                        mostActiveUsersToday += $"    {us.GetDisplayName()} (`{us}`) " +
                        $"(`{MessageCountTodayByUser(user)}` messages)\n";
                    }
                    else
                    {
                        mostActiveUsersToday += $"    Unknown User (`{user.Id}`) " +
                        $"(`{MessageCountTodayByUser(user)}` messages)\n";
                    }
                }

                string info = $"Analytics for **{msg.GetGuild().Name}**\n\n";
                info += $"All Messages Logged: `{days.Sum(d => d.MessageCount)}`\n";
                info += $"All Commands Logged: `{commands.Sum(c => c.Value)}`\n";
                info += $"Most Active Users Overall: \n{mostActiveUsersOverall}";
                info += $"Most Used Commands Overall: \n{MostUsedCommandsOverall}";
                info += $"Most Active Users Today: \n{mostActiveUsersToday}";
                info += $"Users active overall: `{users.Count()}`\n";
                info += $"Users active this month: `{GetTotalThisMonth(users).Count()}`\n";
                info += $"Users active today: `{GetTotalToday(users).Count()}`\n";

                await msg.ReplyAsync(info);
            };

            cmds.Add(analytics);

            return cmds;
        }
        public List<GuildMessageStats.StatsUser> GetTopToday(IEnumerable<GuildMessageStats.StatsUser> users)
        {
            var now = DateTimeOffset.UtcNow;
            var mostActiveIdToday = users.OrderByDescending(
                u => u.Years.Where(y => y.Year == now.Year).Sum(
                    y => y.Months.Where(m => m.Month == now.Month).Sum(
                        m => m.Days.Where(d => d.Day == now.Day).Sum(
                            d => d.MessageCount)))).Take(3);

            return mostActiveIdToday.ToList();
        }
        public List<GuildMessageStats.StatsUser> GetTotalThisMonth(IEnumerable<GuildMessageStats.StatsUser> users)
        {
            var now = DateTimeOffset.UtcNow;
            var result = users.Where(u => u.Years.Any(
                y => y.Year == now.Year && y.Months.Any(
                    m => m.Month == now.Month)));

            return result.ToList();
        }
        public List<GuildMessageStats.StatsUser> GetTotalToday (IEnumerable<GuildMessageStats.StatsUser> users)
        {
            var now = DateTimeOffset.UtcNow;
            var result = users.Where(u => u.Years.Any(
                y => y.Year == now.Year && y.Months.Any(
                    m => m.Month == now.Month && m.Days.Any(
                        d => d.Day == now.Day))));

            return result.ToList();
        }

        public int MessageCountTodayByUser(GuildMessageStats.StatsUser user)
        {
            var now = DateTimeOffset.UtcNow;
            var mostActiveIdToday = user
                .Years.First(y => y.Year == now.Year)
                .Months.First(m => m.Month == now.Month)
                .Days.First(d => d.Day == now.Day).MessageCount;

            return mostActiveIdToday;
        }
    }
}
