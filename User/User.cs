namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


/// <summary>
/// Client example.
/// </summary>
class User
{

    /// <summary>
    /// Logger for this class.
    /// </summary>
    Logger mLog = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configures logging subsystem.
    /// </summary>
    private void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        var console =
            new NLog.Targets.ConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
            };
        config.AddTarget(console);
        config.AddRuleForAllLevels(console);

        LogManager.Configuration = config;
    }

    /// <summary>
    /// Program body.
    /// </summary>
    private void Run()
    {
        //configure logging
        ConfigureLogging();

        //initialize random number generator
        var rnd = new Random();

        //run everythin in a loop to recover from connection errors
        while (true)
        {
            try
            {
                //connect to the server, get service client proxy
                var sc = new ServiceCollection();
                sc
                    .AddSimpleRpcClient(
                        "batteryService", //must be same as on line 72
                        new HttpClientTransportOptions
                        {
                            Url = "http://127.0.0.1:5000/simplerpc",
                            Serializer = "HyperionMessageSerializer"
                        }
                    )
                    .AddSimpleRpcHyperionSerializer();

                sc.AddSimpleRpcProxy<IBatteryService>("batteryService"); //must be same as on line 63

                var sp = sc.BuildServiceProvider();

                var battery = sp.GetService<IBatteryService>();

                //initialize user descriptor
                var user = new ClientDesc();

                //get unique client id
                user.ClientId = battery.GetUniqueId();

                user.ClientType = ClientType.User;

                //log identity data
                mLog.Info($"I am a {user.ClientType}, ID {user.ClientId}.");
                Console.Title = $"I am a {user.ClientType}, ID {user.ClientId}.";

                while (true)
                {
                    bool extracted = false;

                    mLog.Info("I want to extract energy from the battery.");
                    Thread.Sleep(5000);

                    mLog.Info("I am trying to extract energy from the battery");

                    while (!extracted)
                    {
                        var batteryState = battery.GetBatteryStatus();

                        Thread.Sleep(rnd.Next(500));

                        if (batteryState == BatteryStatus.Active)
                        {
                            //try passing 
                            mLog.Info("Battery is active. Trying to extract.");
                            bool isAdjusted = battery.AdjustEnergyLevel(user);

                            //handle result
                            if (isAdjusted)
                            {
                                mLog.Info("Succesfully extracted battery energy.");
                                extracted = true;
                            }
                            else
                            {
                                mLog.Info("Battery energy extraction unsuccessful, checking a little later.");
                                Thread.Sleep(rnd.Next(3000));
                                extracted = false;
                            }
                        }
                        // Could not access battery
                        else
                        {
                            mLog.Info("Couldn't access battery. Going to check later.");
                            Thread.Sleep(rnd.Next(5000));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //log whatever exception to console
                mLog.Warn(e, "Unhandled exception caught. Will restart main loop.");

                //prevent console spamming
                Thread.Sleep(2000);
            }
        }
    }

    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    static void Main(string[] args)
    {
        var self = new User();
        self.Run();
    }
}
