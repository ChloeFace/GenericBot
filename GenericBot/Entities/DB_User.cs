using System;
using System.Collections.Generic;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;

namespace GenericBot.Entities
{
    public class DBUser
    {
        [BsonId]
        public ulong ID { get; set; }
        public List<string> Nicknames { get; set; }
        public List<string> Usernames { get; set; }
        public List<string> Warnings { get; set; }
        public List<ulong> SavedRoles { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public decimal PointsCount { get; set; }
        public DateTimeOffset LastThanks { get; set; }
        public DBUser()
        {

        }
        public DBUser(SocketGuildUser user)
        {
            if (user == null)
                return;
            ID = user.Id;
            Nicknames = new List<string>();
            Usernames = new List<string> { user.Username };
            Warnings = new List<string>();
            PointsCount = 0;
            LastThanks = DateTimeOffset.FromUnixTimeSeconds(0);

            if (!string.IsNullOrEmpty(user.Nickname))
                Nicknames.Add(user.Nickname);
            CreatedAt = user.CreatedAt;
        }

        public DBUser AddUsername(string username)
        {
            if (Usernames == null) Usernames = new List<string>();
            if (!Usernames.Contains(username))
            {
                Usernames.Add(username);
            }
            return this;
        }
        public DBUser AddNickname(string username)
        {
            if (Nicknames == null) Nicknames = new List<string>();
            if (!Nicknames.Contains(username))
            {
                Nicknames.Add(username);
            }
            return this;
        }

        public DBUser AddNickname(SocketGuildUser user)
        {
            if (user == null) return this;
            if (Nicknames == null) Nicknames = new List<string>();
            if (user.Nickname != null && !string.IsNullOrEmpty(user.Nickname) && !Nicknames.Contains(user.Nickname))
            {
                Nicknames.Add(user.Nickname);
            }
            return this;
        }

        public DBUser AddWarning(string warning)
        {
            if (Warnings == null) Warnings = new List<string>();
            Warnings.Add(warning);
            return this;
        }

        public DBUser AddPoints(decimal points)
        {
            PointsCount += points;
            return this;
        }

        public DBUser RemoveWarning(bool allWarnings = false)
        {
            if (Warnings.Empty())
            {
                throw new DivideByZeroException("User has no warnings");
            }
            if (!allWarnings)
            {
                Warnings.RemoveAt(Warnings.Count - 1);
            }
            else
            {
                Warnings.RemoveRange(0, Warnings.Count);
            }
            return this;
        }
    }
}
