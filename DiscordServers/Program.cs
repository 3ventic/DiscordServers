using Discord;
using System;
using System.Linq;

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
        private static Credentials GetCredentials()
        {
            var email = Properties.Settings.Default["Email"].ToString();
            if (!String.IsNullOrEmpty(email))
            {
                char c;
                do
                {
                    Console.WriteLine("Use saved credentials for " + email + "? (y/n) ");
                    c = Char.ToLower(Console.ReadKey().KeyChar);
                    Console.WriteLine();
                }
                while (c != 'y' && c != 'n');

                if (c == 'y')
                {
                    return new Credentials(email, Properties.Settings.Default["Password"].ToString());
                }
            }

            Console.Write("Email: ");
            email = Console.ReadLine();
            Console.Write("Password: ");
            string pass = Console.ReadLine();

            Console.Write("Save these IN PLAIN TEXT for next time? (y/n) ");
            if (Char.ToLower(Console.ReadKey().KeyChar) == 'y')
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
            var creds = GetCredentials();

            var client = new DiscordClient(new DiscordClientConfig());

            client.Connected += async (object sender, EventArgs e) =>
            {
                Console.WriteLine("Connected!");

                var servers = client.AllServers.ToList();
                servers.Sort((a, b) => a.Name.CompareTo(b.Name));

                foreach (var server in servers)
                {
                    Console.WriteLine(server.Name + " => " + server.Id + " - " + server.Region + " - " + server.Members.Count() + " members");
                }

                Console.WriteLine(servers.Count + " servers");

                Console.WriteLine("Disconnecting...");
                try {
                    await client.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!");
                    Console.WriteLine(ex.Message);
                    return;
                }
                Console.WriteLine("Disconnected!");
            };

            client.Run(async () =>
            {
                Console.WriteLine("Connecting...");
                try
                {
                    await client.Connect(creds.Email, creds.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!");
                    Console.WriteLine(ex.Message);
                    return;
                }
            });

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
