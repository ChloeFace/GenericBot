using System.Collections.Generic;
using System.IO;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace GenericBot.Entities
{
    public class GlobalConfiguration
    {
        //Bot Token
        public string Token { get; set; }
        //Database Password
        public string DbConnectionString { get; set; }
        //Default Prefix to be used
        public string DefaultPrefix { get; set; }
        //Default value for executing edited commands
        public bool DefaultExecuteEdits { get; set; }
        //Bot Owner's ID (Let's not use Discord for this, it's slow)
        public ulong OwnerId { get; set; }
        //Global Admins (Have as much power as owner)
        public List<ulong> GlobalAdminIds { get; set; }
        //Status to use
        public string PlayingStatus { get; set; }
        // Wolfram-Alpha Token
        public string WolframAppId { get; set; }

        //Blacklist
        public List<ulong> BlacklistedIds { get; set; }
        //Use debug mode?
        public bool DebugMode { get; set; }

        public GlobalConfiguration()
        {
            Token = "";
            DefaultPrefix = ">";
            DefaultExecuteEdits = true;
            OwnerId = new ulong();
            GlobalAdminIds = new List<ulong>();
            DebugMode = false;

            BlacklistedIds = new List<ulong>();
        }

        public GlobalConfiguration(DiscordSocketClient socketClient, string tok, string pref, bool edit)
        {
            Token = tok;
            DefaultPrefix = pref;
            DefaultExecuteEdits = edit;
            OwnerId = socketClient.GetApplicationInfoAsync().Result.Owner.Id;
            GlobalAdminIds.Add(OwnerId);
            DebugMode = false;

            BlacklistedIds = new List<ulong>();
        }

        public void Save()
        {
            Directory.CreateDirectory("files");

            File.WriteAllText("files/config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public GlobalConfiguration Load()
        {
            return JsonConvert.DeserializeObject<GlobalConfiguration>(File.ReadAllText("files/config.json"));
        }
    }
}
