using System;
using System.Threading.Tasks;

using Asciis;

using Microsoft.Extensions.Logging;

using SlackSkills;
using static SlackSkills.SlackConstants;


namespace EstimateSkills
{
    class Program
    {
        #pragma warning disable 8618
        private SlackApp    _app;
        private CommandLine _config;
        #pragma warning restore 8618

        static async Task Main(string[] args)
        {
            Console.WriteLine(Figlet.Render("SlackSkills"));
            Console.WriteLine(Figlet.Render("Estimate Skills", Figlet.Fonts.ANSIRegular, SmushMode.Kern));

            var program = new Program();

            program.SetupSlackApp(args);
            program.RegisterMessageTypes();

            await program.Run();
        }

        private void SetupSlackApp(string[] args)
        {
            // Parse any command line arguments and environment variables
            _config = ParamParser<CommandLine>.Parse(args);

            // Setup Slack integration
            // Depending on the scenario, you will need to supply only some fields
            //  - SocketMode app.          AppLevelToken from https://api.slack.com/apps/{appId}/general
            //  - Make calls as a Bot.     BotAccessToken or (ClientId + ClientSecret + BotScopes + RedirectUrl for browser login)
            //  - Make calls as a User.    UserAccessToken or ((ClientId + ClientSecret + UserScopes + RedirectUrl for browser login)
            // RedirectUrl is optional for browser login. The default url will be http://localhost:3100/ if it is not supplied
            //   This url must match the Redirect URLs in your app settings: https://api.slack.com/apps/{appId}/oauth
            _app = new SlackApp
            {
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret,
                AppLevelToken = _config.AppLevelToken,
                BotAccessToken = _config.BotAccessToken,
                BotScopes = _config.BotScopes ?? DefaultBotScope,
                UserAccessToken = _config.UserAccessToken,
                UserScopes = _config.UserScopes ?? DefaultUserScope,
                RedirectUrl = _config.RedirectUrl ?? DefaultRedirectUrl,
                AccessTokensUpdated = slackApp =>
                {
                    // If you aren't supplying a bot or user access token, 
                    // After the user logs in using the browser
                    Console.WriteLine($"Bot Access Token: {slackApp.BotAccessToken}");
                    Console.WriteLine($"User Access Token: {slackApp.UserAccessToken}");
                    // TODO: Save new access tokens to a safe place
                },
                LogBuilder = builder =>
                {
                    // Microsoft Logging framework. Configure as you feel best
                    // TODO: configure the Logger builder
                    builder
#if DEBUG
                                          .SetMinimumLevel(LogLevel.Debug)
#endif
                                          .AddDebug()
                       .AddConsole(options =>
                       {
                           options.LogToStandardErrorThreshold = LogLevel.Debug;
                       });
                }
            };
        }

        /// <summary>
        ///     Console loop
        /// </summary>
        private async Task Run()
        {
            // Setup is done, now
            // Connect to slack
            var success = await _app.Connect();
            if (!success)
            {
                var help = new ParamParser<CommandLine>().Help();
                Console.WriteLine(help);

                return;
            }

            // Hold the console open
            do
            {
                Console.WriteLine("Enter 'Stop' to exit");
                var line = Console.ReadLine();

                if (string.Compare(line, "Stop", StringComparison.OrdinalIgnoreCase) == 0)
                    break;

                if (string.Compare(line, "cls", StringComparison.OrdinalIgnoreCase) == 0)
                    Console.Clear();

            } while (true);

            Console.WriteLine("Slack CLI Stopped");
        }

        public void RegisterMessageTypes()
        {
            _app.RegisterSlashCommand<EstimateSlashCommand>();
            _app.RegisterMessageShortcutCommand<EstimateMessageShortcutCommand>();
            _app.OnMessage<Message>(OnMessage);
        }

        private async void OnMessage(ISlackApp slackApp, Message msg)
        {
            if (msg.text != null)
            {
                if (AnnoyingGreeting(msg.text))
                {
                    var context = await slackApp.GetUserContext(msg.user);
                    var name    = context?.User.real_name ?? context?.User.name ?? "";
                    _ = slackApp.AddReaction(msg, "see_no_evil");
                    _ = slackApp.Say($"http://www.nohello.com Please visit this site, {name}.",
                                     msg.channel, user: msg.user, ts: msg.thread_ts);
                }
            }
        }

        private static bool AnnoyingGreeting(string text)
        {
            return (text.IgnoreCaseEquals("hi")
                 || text.IgnoreCaseEquals("hello")
                 || text.IgnoreCaseEquals("gidday"));
        }

        // Open up you https://api.slack.com/apps/  Event subscriptions. Update to include *ONLY* the scopes you need
        private static readonly string DefaultBotScope = BuildScope(
                                                                    SlackConstants.BotScopes.Commands,
                                                                    SlackConstants.BotScopes.Channels_History,
                                                                    SlackConstants.BotScopes.Chat_Write,
                                                                    SlackConstants.BotScopes.Groups_History,
                                                                    SlackConstants.BotScopes.Reactions_Write,
                                                                    SlackConstants.BotScopes.Users_Read
                                                                    );

        private static readonly string DefaultUserScope = BuildScope();
    }
}
