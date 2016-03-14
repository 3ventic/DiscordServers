using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordServers
{
    struct Credentials
    {
        public readonly string Email;
        public readonly string Password;

        public Credentials(string email, string pass)
        {
            Email = email;
            Password = pass;
        }
    }

    class Program
    {
        private static DiscordClient _client = new DiscordClient(new DiscordClientConfig());

        private static Credentials GetCredentials(bool forceUseSaved)
        {
            var email = Properties.Settings.Default["Email"].ToString();
            if (!string.IsNullOrEmpty(email))
            {
                if (forceUseSaved)
                {
                    return new Credentials(email, Properties.Settings.Default["Password"].ToString());
                }
                else
                {
                    char c;
                    do
                    {
                        Console.WriteLine("Use saved credentials for " + email + "? (y/n) ");
                        c = char.ToLower(Console.ReadKey().KeyChar);
                        Console.WriteLine();
                    }
                    while (c != 'y' && c != 'n');

                    if (c == 'y')
                    {
                        return new Credentials(email, Properties.Settings.Default["Password"].ToString());
                    }
                }
            }

            Console.Write("Email: ");
            email = Console.ReadLine();
            Console.Write("Password: ");
            string pass = Console.ReadLine();

            Console.Write("Save these IN PLAIN TEXT for next time? (y/n) ");
            if (char.ToLower(Console.ReadKey().KeyChar) == 'y')
            {
                Properties.Settings.Default["Email"] = email;
                Properties.Settings.Default["Password"] = pass;
                Properties.Settings.Default.Save();
                Console.WriteLine("\nSaved!");
            }
            else
            {
                Console.WriteLine("\nNot saved!");
            }
            return new Credentials(email, pass);
        }

        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var creds = GetCredentials(args.Length > 0);

            _client.Connected += (object sender, EventArgs e) =>
            {
                Console.WriteLine("Connected!");
                ListenToUserInput(args);
            };

            _client.MessageReceived += Client_MessageReceived;

            _client.Run(async () =>
            {
                Console.WriteLine("Connecting...");
                try
                {
                    await _client.Connect(creds.Email, creds.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!");
                    Console.WriteLine(ex.Message);
                    return;
                }
            });
            ListenToUserInput(args);
        }

        private static async void ListenToUserInput(string[] args)
        {
            bool listen = true;

            foreach (var arg in args)
            {
                ProcessUserCommand(arg).Wait();
                if (arg == "exit")
                {
                    listen = false;
                    try
                    {
                        _client.Disconnect().Wait();
                    }
                    catch (AggregateException) { }
                    Environment.Exit(0);
                }
            }

            if (listen)
            {
                Console.WriteLine("Press ^C to exit.");
                await Task.Run(() =>
                {
                    while (true)
                    {
                        Console.Write("> ");
                        ProcessUserCommand(Console.ReadLine()).Wait();
                    }
                });
            }
        }

        private static async Task ProcessUserCommand(string command)
        {
            var cmdparts = SplitWithQuotes(command);
            switch (cmdparts[0])
            {
                case "help":
                    {
                        Console.WriteLine("listservers listchannels findchannel");
                    }
                    break;
                case "listservers":
                    {
                        var servers = _client.AllServers.ToList();
                        servers.Sort((a, b) => a.Name.CompareTo(b.Name));

                        foreach (var server in servers)
                        {
                            Console.WriteLine($"Server Name {server.Name} ID {server.Id} Region {server.Region} Membercount {server.Members.Count()}");
                        }

                        Console.WriteLine(servers.Count + " servers");
                    }
                    break;
                case "listchannels":
                    if (cmdparts.Length > 0)
                    {
                        long id;
                        if (long.TryParse(cmdparts[1], out id))
                        {
                            var server = _client.GetServer(id);
                            if (server != null)
                            {
                                PrintServerChannels(server);
                            }
                            else
                            {
                                Console.WriteLine($"No server with ID {id}");
                            }
                        }
                        else
                        {
                            var servers = _client.FindServers(cmdparts[1]);

                            foreach (var server in servers)
                            {
                                PrintServerChannels(server);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Server ID or Name null");
                    }
                    break;
                case "getchathistory":
                    if (cmdparts.Length > 0)
                    {
                        long id;
                        if (long.TryParse(cmdparts[1], out id))
                        {
                            await PrintMessageHistory(id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid ID {cmdparts[1]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid ID null");
                    }
                    break;
            }
        }

        private static async Task PrintMessageHistory(long id, long? relativeId = null)
        {
            var response = (await _client.API.GetMessages(id, 50, relativeId)).OrderByDescending(x => x.Timestamp).ToList();
            foreach (var message in response)
            {
                DateTime timestamp = message.Timestamp ?? DateTime.UtcNow;
                Console.WriteLine($"[{timestamp.ToUniversalTime().ToString("u")}] <{message.Author.Username}> {message.Content}");
            }
            if (response.Count > 0)
            {
                await PrintMessageHistory(id, response.Last().MessageId);
            }
        }

        private static void PrintServerChannels(Server server)
        {
            Console.WriteLine($"Server ID {server.Id} Name {server.Name} Default Channel ID {server.DefaultChannel.Id} Name {server.DefaultChannel.Name}");
            foreach (var channel in server.TextChannels)
            {
                Console.WriteLine($"Text Channel ID {channel.Id} Name {channel.Name} Private {channel.IsPrivate}");
            }
            foreach (var channel in server.VoiceChannels)
            {
                Console.WriteLine($"Text Channel ID {channel.Id} Name {channel.Name} Private {channel.IsPrivate}");
            }
        }

        private static void Client_MessageReceived(object sender, MessageEventArgs e)
        {
        }

        private static string[] SplitWithQuotes(string original)
        {
            return original.Split('"').Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { element }).SelectMany(element => element).ToList().ToArray();
        }
    }
}
