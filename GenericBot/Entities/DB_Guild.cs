using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MongoDB.Driver;
using MongoDB.Bson;

namespace GenericBot.Entities
{
    public class DBGuild
    {
        public ulong ID { get; set; }
        public List<DBUser> Users
        {
            get
            {
                var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
                var users = gDb.GetCollection<DBUser>("users");
                return users.AsQueryable().ToList();
            }
        }
        public List<Quote> Quotes
        {
            get
            {
                var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
                var quotes = gDb.GetCollection<Quote>("users");
                return quotes.AsQueryable().ToList();
            }
        }

        public DBGuild()
        {

        }

        public DBGuild(ulong guildId)
        {
            this.ID = guildId;
        }

        public void AddOrUpdateUser(DBUser user)
        {

            var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var users = gDb.GetCollection<DBUser>("users");
            if(users.CountDocuments(Builders<DBUser>.Filter.Eq("ID" ,user.ID)) == 1)
            {
                users.DeleteOne(Builders<DBUser>.Filter.Eq("ID", user.ID));
            }
            users.InsertOne(user);
        }

        public DBUser GetOrCreateUser(ulong userId)
        {
            var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var users = gDb.GetCollection<DBUser>("users");
            if (users.CountDocuments(u => u.ID == userId) > 0)
            {
                return users.Find(Builders<DBUser>.Filter.Eq("ID", userId)).ToList().First();
            }
            else
            {
                if(GenericBot.DiscordClient.GetGuild(this.ID).GetUser(userId) != null)
                    users.InsertOne(new DBUser(GenericBot.DiscordClient.GetGuild(this.ID).GetUser(userId)));
                else
                    users.InsertOne(new DBUser() { ID = userId });

                return GetOrCreateUser(userId);
            }
        }

        public Quote AddQuote(string content)
        {
            var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var Quotes = gDb.GetCollection<Quote>("quotes");

            var q = new Quote
            {
                Content = content,
                Id = Quotes.CountDocuments(new BsonDocument()) == 0 ? 1 : (int)Quotes.CountDocuments(new BsonDocument()) + 1,
                Active = true
            };
            Quotes.InsertOne(q);

            return q;
        }

        public Quote GetQuote(string identifer)
        {
            var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var dbQuotes = gDb.GetCollection<Quote>("quotes");

            try
            {
                if (!gDb.ListCollectionNames(new ListCollectionNamesOptions { Filter = new BsonDocument("name", "quotes")}).Any()
                    || dbQuotes.CountDocuments(new BsonDocument("Active", false)) == 0)
                {
                    return new Quote { Content = "This server has no quotes", Id = -1 };
                }

                var Quotes = dbQuotes.Find(new BsonDocument()).ToList();

                if (string.IsNullOrEmpty(identifer))
                {
                    var ql = Quotes.Where(q => q.Active);
                    int max = ql.Count();
                    return ql.ElementAt(new Random().Next(0, max));
                }
                else
                {
                    if (int.TryParse(identifer.Trim(), out int id))
                    {
                        if (Quotes.Last(q => q.Active).Id >= id)
                        {
                            var quote = Quotes.Find(q => q.Id.Equals(id));
                            if (quote.Active)
                            {
                                return quote;
                            }
                            else
                            {
                                return new Quote { Content = "Quote deleted", Id = id };
                            }
                        }
                        else
                        {
                            return new Quote { Content = "Could not find quote", Id = -2 };
                        }
                    }
                    else
                    {
                        var ql = Quotes.Where(q => q.Active)
                            .Where(q => !string.IsNullOrEmpty(q.Content.Trim())).Where(q => q.Content.ToLower().Contains(identifer.ToLower()));
                        if (ql.Count() == 0)
                        {
                            return new Quote { Content = "Could not find quote", Id = -1 };
                        }

                        int max = ql.Count();
                        return ql.ElementAt(new Random().Next(0, max));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return new Quote { Content = "Could not find quote", Id = -0 };
            }
        }

        public bool RemoveQuote(int id)
        {
            var gDb = GenericBot.mongoClient.GetDatabase($"{this.ID}");
            var dbQuotes = gDb.GetCollection<Quote>("quotes");

            try
            {
                dbQuotes.UpdateOne(new BsonDocument("ID", id), Builders<Quote>.Update.Set("Active", false));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
