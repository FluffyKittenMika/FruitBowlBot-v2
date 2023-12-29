using FruitBowlBot_v2.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Message = FruitBowlBot_v2.Commands.Message;

namespace FruitBowlBot_v2.ConnectionSystems
{
    class Program

    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Bot bot = new();
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
           
            }
          
        }
    }

    class Bot
    {
        public static Dictionary<string, string> settings = new();
        public static List<IPluginCommand> _plugins = new();

        TwitchClient client;

      



        public Bot()
        {

            #region config loading
            var settingsFile = @"./Config/Config.txt";
            if (File.Exists(settingsFile)) //Check if the Config file is there, if not, eh, whatever, break the program.
            {
                using (StreamReader r = new(settingsFile))
                {
                    string line; //keep line in memory outside the while loop
                    while ((line = r.ReadLine()) != null)
                    {
                        try
                        {
                            if ( line == "" || line[0] != '#' )//skip comments
                            {
                                string[] split = line.Split('='); //Split the non comment lines at the equal signs
                                settings.Add(split[0], split[1]); //add the first part as the key, the other part as the value
                                                                  //now we got stuff callable like so " settings["username"]  "  this will return the username value.
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine("This error is stupid, it hates spacebars");
                        }
                     
                    }
                }

                Console.WriteLine("Detected settings for these keys");
                foreach (var item in settings.Keys)
                    Console.WriteLine(item);

            }
            else
            {
                Console.Write("nope, no config file found, please craft one from the example");
                Thread.Sleep(5000);
                Environment.Exit(0); // Closes the program if there's no setting, should just make it generate one, but as of now, don't delete the settings.
            }
            #endregion

            #region plugins
            Console.WriteLine("Loading Plugins");
            try
            {
                // Magic to get plugins
                var pluginCommand = typeof(IPluginCommand);
                var pluginCommands = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => pluginCommand.IsAssignableFrom(p) && p.BaseType != null);

                foreach (var type in pluginCommands)
                {
                    _plugins.Add((IPluginCommand)Activator.CreateInstance(type));
                }
                var commands = new List<string>();
                foreach (var plug in _plugins)
                {
                    if (!commands.Contains(plug.Command))
                    {
                        commands.Add(plug.Command);
                        if (plug.Loaded)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Loaded: {plug.PluginName}");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"NOT Loaded: {plug.PluginName}");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"NOT Loaded: {plug.PluginName} Main command conflicts with another plugin!!!");
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.InnerException);
#endif
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            #endregion





            ConnectionCredentials credentials = new(settings["username"], settings["accesstoken"]);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, settings["channel"]);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;
            client.OnRateLimit += Client_RateLimited;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;

            client.Connect();
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var chatClient = (TwitchClient)sender;
            var enabledPlugins = _plugins.Where(plug => plug.Loaded).ToArray();
            var command = e.Command.CommandText.ToLower();
            Message msg = new()
            {
                Arguments = e.Command.ArgumentsAsList,
                Command = command,
                Channel = e.Command.ChatMessage.Channel,
                IsModerator = (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator), // fixes some issues
                RawMessage = e.Command.ChatMessage.Message,
                Username = e.Command.ChatMessage.Username
            };

            //just a hardcoded command for enabling / disabling plugins
            if (command == "plugin" && msg.IsModerator)
                chatClient.SendMessage(e.Command.ChatMessage.Channel, PluginManager(msg));

            foreach (var plug in enabledPlugins)
            {
                if (plug.Command == command || plug.Aliases.Contains(command))
                {
                    string reaction = "";
                    try
                    {
                        reaction = plug.Action(msg).Result;
                    }
                    catch (Exception errr)
                    {
                        Console.WriteLine(errr.Message + " ---- " + errr.StackTrace);
                    }

                    if (reaction != null)
                        chatClient.SendMessage(e.Command.ChatMessage.Channel, reaction);
                    break;
                }//do nothing if no match
            }
        }

        /// <summary>w
		/// turns off or on a plugin based on its name
		/// </summary>
		/// <param name="message"></param>
		/// <returns>result string</returns>
		private string PluginManager(Message message)
        {
            var plugin = "";
            var toggle = true;

            if (message.Arguments.Count > 0 && message.Arguments.Count < 3)
            {
                if (message.Arguments.ElementAtOrDefault(0) != null)
                    plugin = message.Arguments[0];
                if (message.Arguments.ElementAtOrDefault(1) != null)
                    toggle = Convert.ToBoolean(message.Arguments[1]);

                IPluginCommand[] plugs = _plugins.Where(plug => plug.Command == plugin).ToArray();
                IPluginCommand[] plugsalas = _plugins.Where(plug => plug.Aliases.Contains(plugin)).ToArray();
                IPluginCommand[] combined = new IPluginCommand[plugs.Length + plugsalas.Length];

                Array.Copy(plugs, combined, plugs.Length);
                Array.Copy(plugsalas, 0, combined, plugs.Length, plugsalas.Length);

                if (combined.Count() > 0)
                {
                    combined[0].Loaded = toggle;
                    string status = toggle ? "Enabled" : "Disabled";
                    return $"{combined[0].PluginName} is now {status}";
                }
                else
                    return $"Could not find a plugin with the command {plugin}";
            }
            return "You must define a plugin command, and a bool";
        }

        private void Client_RateLimited(object sender, OnRateLimitArgs e)
        {
            Console.WriteLine($"Rate limited error, chillout");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            if (settings["logging"] == "true")
            {
                Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
            }
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Connected to {settings["channel"]}");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{DateTime.Now.TimeOfDay}:{e.ChatMessage.Channel}:{e.ChatMessage.Username}:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{e.ChatMessage.Message}");
            Console.ForegroundColor = ConsoleColor.White;

            if (settings["annoyotron"] == "true")
            {

                //TODO: buggy, 2x execute
                new Thread(delegate ()
                {
                    Application.EnableVisualStyles();

                    Forms.Message a = new(e);
                    Application.Run(a);
                }).Start();

                // MessageBox.Show(e.ChatMessage.Message, $"{e.ChatMessage.Username} says:");
            }

            if (settings["speakchat"] == "true")
            {
                var sayit = _plugins.Where(plug => plug.PluginName == "say").FirstOrDefault();
                var reaction = "";
                try
                {
                    Message msg = new()
                    {
                        Arguments = e.ChatMessage.Message.Split(' ').ToList(),
                        Command = e.ChatMessage.Message,
                        Channel = e.ChatMessage.Channel,
                        IsModerator = (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator), // fixes some issues
                        RawMessage = e.ChatMessage.Message,
                        Username = e.ChatMessage.Username
                    };
                    reaction = sayit.Action(msg).Result;
                }
                catch (Exception errr)
                {
                    Console.WriteLine(errr.Message + " ---- " + errr.StackTrace);
                }

            }


            //    if (e.ChatMessage.Message.Contains("bagle"))
            //    client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromSeconds(1), "Bad word! 1sec timeout!");

            if (e.ChatMessage.Message.Contains("!quote"))
            {
                client.SendMessage(e.ChatMessage.Channel, $"no :)");
            }

            if (e.ChatMessage.Message.Contains("!timeout"))
            {
                client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromSeconds(1), "Affirmative");
            }




        }


        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"Thanks {e.Subscriber.DisplayName} for the bread!");
            else
                client.SendMessage(e.Channel, $"Thanks {e.Subscriber.DisplayName} I can have more bread now!");
        }
    }
}
