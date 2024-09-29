namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


/// <summary>
/// Charger client.
/// </summary>
class Charger
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

        //run everything in a loop to recover from connection errors
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

                //initialize client descriptor
                var charger = new ClientDesc();

                //get unique user id
                charger.ClientId = battery.GetUniqueId();

                charger.ClientType = ClientType.Charger;

                //log identity data
                mLog.Info($"I am a {charger.ClientType}, ID {charger.ClientId}.");
                Console.Title = $"I am a {charger.ClientType}, ID {charger.ClientId}.";

                while (true)
                {
                    bool extracted = false;

                    mLog.Info("I want to charge the battery.");
                    Thread.Sleep(5000);

                    mLog.Info("I am trying to charge the battery.");

                    while (!extracted)
                    {
                        var batteryState = battery.GetBatteryStatus();

                        Thread.Sleep(rnd.Next(500));

                        if (batteryState == BatteryStatus.Active)
                        {
                            //try passing 
                            mLog.Info("Battery is active. Trying to charge.");
                            bool isAdjusted = battery.AdjustEnergyLevel(charger);

                            //handle result
                            if (isAdjusted)
                            {
                                mLog.Info("Succesfully charged battery.");
                                extracted = true;
                            }
                            else
                            {
                                mLog.Info("Battery charge unsuccessful, checking a little later.");
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
        var self = new Charger();
        self.Run();
    }
}
