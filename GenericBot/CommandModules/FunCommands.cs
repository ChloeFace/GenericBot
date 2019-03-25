using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    public class FunCommands
    {
        public List<Command> GetFunCommands()
        {
            List<Command> FunCommands = new List<Command>();

            Command markov = new Command("markov");
            markov.Description = "Create a markov chain from the last messages in the channel";
            markov.Delete = true;
            markov.Usage = "markov";
            markov.ToExecute += async (client, msg, parameters) =>
            {
                var messages = msg.Channel.GetMessagesAsync().Flatten().Reverse().Select(m => m.Content).ToList().Result;
                messages.ToList().AddRange(messages.TakeLast(50));
                messages.ToList().AddRange(messages.TakeLast(25));
                messages.ToList().AddRange(messages.TakeLast(10));

                int averageLength = messages.Sum(m => m.Split(' ').Length) / 185;
                averageLength = averageLength > 10 ? averageLength : averageLength * 2;

                var markovGenerator = new SharperMark.LookbackMarkov();
                markovGenerator.Train(messages.ToArray());

                await msg.ReplyAsync(markovGenerator.GenerateWords(averageLength));
            };

            FunCommands.Add(markov);

            Command wat = new Command("wat");
            wat.Description = "The best command";
            wat.ToExecute += async (client, msg, parameters) =>
            {
                await msg.ReplyAsync($"**-wat-**\nhttp://destroyallsoftware.com/talks/wat");
            };

            FunCommands.Add(wat);

            Command twobee = new Command("2b");
            twobee.Description = "toggle the command";
            twobee.RequiredPermission = Command.PermissionLevels.Admin;
            twobee.ToExecute += async (client, msg, parameters) =>
            {
                if(msg.Author.Id == 189378507724292096)
                {
                    await msg.ReplyAsync($"2b sleep");
                }
                else
                {
                    GenericBot.annoy2B = !GenericBot.annoy2B ;
                    await msg.ReplyAsync($"toggled to " + GenericBot.annoy2B);
                }
                
            };
            FunCommands.Add(twobee);

            Command inspirobot = new Command("inspirobot");
            inspirobot.Description = "Generate a new image from Inspirobot";
            inspirobot.ToExecute += async (client, msg, parameters) =>
            {
                await msg.ReplyAsync(new WebClient().DownloadString("http://inspirobot.me/api?generate=true"));
            };

            FunCommands.Add(inspirobot);

            Command ud = new Command("ud");
            ud.Aliases = new List<string> { "urbandictionary" };
            ud.Description = "Define a word or phrase with UrbanDictionary";
            ud.ToExecute += async (client, msg, parameters) => 
            {
                string word = parameters.reJoin();
                var urbanclient = new UrbanDictionary.UrbanClient();
                var urbanResponse = urbanclient.GetClientResponse(word);

                var em = new EmbedBuilder();

                em.WithThumbnailUrl("http://marcmarut.com/wp-content/uploads/2013/12/Urban-Dictionary-Icon3.png");
                Regex rgx = new Regex(@"\[(\w|\s)*\]");
                if (urbanResponse.List.Count == 0)
                {
                    em.WithTitle(word);
                    em.WithDescription("No matches found");
                    em.WithColor(new Color(250, 0, 0));
                }
                else
                {
                    var pos = new Random().Next(urbanResponse.List.Where(w => (w.ThumbsUp - w.ThumbsDown) > 0).ToList().Count);
                    var wordToUse = urbanResponse.List.Where(w => (w.ThumbsUp - w.ThumbsDown) > 0).ToList()[pos];

                    em.WithTitle(wordToUse.Word);
                    em.WithUrl(wordToUse.Permalink);
                    em.AddField("Definition", wordToUse.Definition.Replace("[", "").Replace("]", ""));
                    em.AddField("Example", wordToUse.Example.Replace("[", "").Replace("]", ""));
                    //em.AddField("Tags", wordToUse.Tags.Aggregate((i, j) => i + ", " + j));
                    em.AddField("Author", wordToUse.Author, inline: true);
                    em.AddField("Rating", wordToUse.ThumbsUp - wordToUse.ThumbsDown, inline: true);
                    em.WithFooter(new EmbedFooterBuilder().WithText($" Definition {pos + 1}/{urbanResponse.List.Count} ({urbanResponse.List.Where(w => (w.ThumbsUp - w.ThumbsDown) > 0).ToList().Count})"));
                    em.WithColor(new Color(19, 79, 230));
                }

                await msg.Channel.SendMessageAsync("", embed: em.Build());
            };
            FunCommands.Add(ud);
            Command roll = new Command("roll");
            roll.Aliases.Add("dice");
            roll.Description = "Roll a specified number of dices. Defaults to 1d20 if no parameters";
            roll.Usage = "roll [count]d[sides]";
            roll.ToExecute += async (client, msg, parameters) =>
            {
                uint count = 1;
                uint sides = 20;
                int add = 0;
                if (!parameters.Empty()) // If there are any parameters
                {
                    string param = parameters.reJoin("").ToLower();
                    if (!param.Contains("d")) // it's just a number
                    {
                        if (!uint.TryParse(param, out count))
                        {
                            await msg.ReplyAsync("Input improperly formatted");
                            return;
                        }
                    }
                    else
                    {
                        if (!(param.Contains("+") || param.Contains("-"))) // There's no modifier
                        {
                            var list = param.Split('d');
                            if (!uint.TryParse(list[1], out sides))
                            {
                                await msg.ReplyAsync("Input improperly formatted");
                                return;
                            }
                            uint.TryParse(list[0], out count);
                            count = (count <= 1 ? 1 : count);
                        }
                        else
                        {
                            string c = param.Split('d')[0];
                            string second = param.Split('d')[1];
                            string s = "";
                            string a = "";
                            if (param.Contains("+"))
                            {
                                s = second.Split('+')[0];
                                a = second.Split('+')[1];
                            }
                            else if (param.Contains("-"))
                            {
                                s = second.Split('-')[0];
                                a = second.Replace(s, "");
                            }

                            if (!(uint.TryParse(c, out count) && uint.TryParse(s, out sides) && int.TryParse(a, out add)))
                            {
                                await msg.ReplyAsync("Input improperly formatted");
                                return;
                            }
                        }
                    }
                }

                if (count > 100)
                {
                    await msg.ReplyAsync("I don't think you can hold that many!");
                    return;
                }
                if (sides < 2)
                {
                    await msg.ReplyAsync("You can't have a 1-sided dice!");
                    return;
                }
                if (sides > 100)
                {
                    await msg.ReplyAsync("That's an awefully large dice, I can't hold that");
                    return;
                }
                RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
                List<int> results = new List<int>();
                while (results.Count < count)
                {
                    byte[] bytes = new byte[4];
                    crypto.GetNonZeroBytes(bytes);
                    long rand = Math.Abs(BitConverter.ToInt32(bytes, 0)) % sides;
                    results.Add((int)rand + 1 + add);
                }

                string res = $"{msg.Author.Mention}, you rolled ";
                results.Sort();
                res += results.SumAnd();
                if (count > 1) res += $" with a total of {results.Sum()}";
                await msg.ReplyAsync(res);
            };

            FunCommands.Add(roll);

            Command cat = new Command("cat");
            cat.Description = "Link a cat pic";
            cat.SendTyping = false;
            cat.ToExecute += async (client, msg, parameters) =>
            {
                try
                {
                    await msg.ReplyAsync(GenericBot.Animols.GetCat());
                }
                catch
                {
                    await msg.ReplyAsync("Uh oh, something borked a bit. Wait a sec and try again.");
                }
                GenericBot.Animols.RenewCats();
            };
            FunCommands.Add(cat);

            Command dog = new Command("dog");
            dog.Description = "Link a dog pic";
            dog.SendTyping = false;
            dog.ToExecute += async (client, msg, parameters) =>
            {
                await msg.ReplyAsync(GenericBot.Animols.GetDog());
                GenericBot.Animols.RenewDogs();
            };
            FunCommands.Add(dog);

            Command floof = new Command("floof");   // defines a new command for the bot called floof
            floof.Description = "link a pic of chefs dog";  //adds a description so the help command works
            floof.ToExecute += async (client, msg, parameters) =>  //i dont actually know what this does tbh but it seems important (maybe is the section of code that is run when the command is called (probably that tbh))
            {
                string baseURL = "https://mastrchef.rocks/baloo/";
                string floofstring;
                using (var wc = new System.Net.WebClient())
                    floofstring = wc.DownloadString(baseURL + "list.php");
                var floofarray = floofstring.Split("<br>").ToList();
                floofstring = floofarray.GetRandomItem();
                await msg.ReplyAsync(baseURL + floofstring);
            };
            FunCommands.Add(floof);

           
            Command addQuote = new Command("addQuote");
            addQuote.SendTyping = false;
            addQuote.Description = "Add a quote to the server's list";
            addQuote.ToExecute += async (client, msg, parameters) =>
            {
                var dbGuild = new DBGuild(msg.GetGuild().Id);
                if(string.IsNullOrEmpty(parameters.reJoin()))
                {
                    await msg.ReplyAsync("You can't add an empty quote");
                    return;
                }
                var q = dbGuild.AddQuote(parameters.reJoin());
                dbGuild.Save();
                await msg.ReplyAsync($"Added {q.ToString()}");
            };
            FunCommands.Add(addQuote);

            Command removeQuote = new Command("removeQuote");
            removeQuote.Description = "Remove a quote from the server's list";
            removeQuote.SendTyping = false;
            removeQuote.RequiredPermission = Command.PermissionLevels.Moderator;
            removeQuote.ToExecute += async (client, msg, parameters) =>
            {
                if (parameters.Empty())
                {
                    await msg.ReplyAsync("You must supply a number");
                    return;
                }

                var dbGuild = new DBGuild(msg.GetGuild().Id);
                if (int.TryParse(parameters[0], out int quid))
                {
                    if (dbGuild.RemoveQuote(quid))
                    {
                        await msg.ReplyAsync($"Succefully removed quote #{quid}");
                    }
                    else
                    {
                        await msg.ReplyAsync($"The number was greater than the number of quotes saved");
                        return;
                    }
                }
                else
                {
                    await msg.ReplyAsync("You must pass in a number");
                }
                dbGuild.Save();
            };
            FunCommands.Add(removeQuote);

            Command quote = new Command("quote");
            quote.SendTyping = false;
            quote.Description = "Get a random quote from the server's list";
            quote.ToExecute += async (client, msg, parameters) =>
            {
                if (!parameters.Empty() && parameters[0] == "all" && quote.GetPermissions(msg.Author, msg.GetGuild().Id) >=
                    Command.PermissionLevels.Admin)
                {
                    System.IO.File.WriteAllText("quotes.txt", new DBGuild(msg.GetGuild().Id).Quotes.Where(q => q.Active).Select(q => $"{q.Content} (#{q.Id})").Aggregate((i, j) => i + "\n" + j));
                    await msg.Channel.SendFileAsync("quotes.txt");
                    System.IO.File.Delete("quotes.txt");
                    return;
                }
                await msg.ReplyAsync(new DBGuild(msg.GetGuild().Id).GetQuote(parameters.reJoin()));
            };
            FunCommands.Add(quote);

            Command redact = new Command(nameof(redact));
            redact.ToExecute += async (client, msg, parameters) =>
            {
                await msg.DeleteAsync();
                char block = '█';
                int rcont = 1;
                StringBuilder resp = new StringBuilder();
                foreach (var str in parameters)
                {
                    int rand = new Random().Next(0, rcont) + 1;
                    if (rand == rcont)
                    {
                        resp.Append(' ');
                        resp.Append(str);
                        resp.Append(' ');
                        rcont = 1;
                    }
                    else
                    {
                        resp.Append(new String(block, str.Length));
                        if (rcont == 16) rcont = 1;
                    }
                    rcont++;
                }

                await msg.ReplyAsync(resp.ToString().Replace("  ", " "));
            };

            FunCommands.Add(redact);

            Command clap = new Command("clap");
            clap.Usage = "Put the clap emoji between each word";
            clap.ToExecute += async (client, msg, parameters) =>
            {
                await msg.ReplyAsync(parameters.reJoin(" :clap: ") + " :clap:");
            };

            FunCommands.Add(clap);

            return FunCommands;
        }
    }
}
