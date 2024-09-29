namespace Servers;

using NLog;

using Services;

/// <summary>
/// Base battery unchanging specifications.
/// </summary>
public static class BatterySpecs
{
	public const int MaxBatteryTemperature = 120;
	public const int MinBatteryTemperature = 0;
	public const int MaxBatteryEnergy = 100;
	public const int MinBatteryEnergy = 0;
}

/// <summary>
/// Battery state descritor.
/// </summary>
public class Battery
{
	/// <summary>
	/// Access lock.
	/// </summary>
	public readonly object AccessLock = new object();

	/// <summary>
	/// Last unique ID value generated.
	/// </summary>
	public int LastUniqueId;

	/// <summary>
	/// Battery status (active or overheated).
	/// </summary>
	public BatteryStatus BatteryStatus;

	/// <summary>
	/// Initial temperature.
	/// </summary>
	public int BatteryTemperature = 20;

	/// <summary>
	/// Initial energy.
	/// </summary>
	public int BatteryEnergy = 20;

}

/// <summary>
/// <para> Battery logic. </para>
/// <para> Thread safe. </para>
/// </summary>
class BatteryLogic
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Background task thread.
	/// </summary>
	private Thread mBgTaskThread;

	/// <summary>
	/// State descriptor.
	/// </summary>
	private Battery mState = new Battery();

	/// <summary>
	/// Constructor.
	/// </summary>
	public BatteryLogic()
	{
		mBgTaskThread = new Thread(BackgroundCooling);
		mBgTaskThread.Start();
	}

	/// <summary>
	/// Battery temperature cooling function.
	/// </summary>
	public void CoolBattery()
	{
		var rnd = new Random();
		mLog.Info($"Battery is trying to cool itself");

		if (mState.BatteryTemperature <= BatterySpecs.MinBatteryTemperature)
		{
			mLog.Info($"There is no need for cooling. Battery temperature is {mState.BatteryTemperature}");
			return;
		}

		var amount = rnd.Next(1, 10);
		switch (mState.BatteryStatus)
		{
			case BatteryStatus.Active:
				lock (mState.AccessLock)
				{
					if (mState.BatteryTemperature - amount < BatterySpecs.MinBatteryTemperature)
					{
						mState.BatteryTemperature = BatterySpecs.MinBatteryTemperature;
					}
					else
					{
						mState.BatteryTemperature -= amount;
					}
				}
				break;

			case BatteryStatus.Overheated:
				if (mState.BatteryTemperature - amount < BatterySpecs.MinBatteryTemperature)
				{
					mState.BatteryTemperature = BatterySpecs.MinBatteryTemperature;
				}
				else
				{
					mState.BatteryTemperature -= amount;
				}
				break;
		}
		mLog.Info($"Battery cooled itself to {mState.BatteryTemperature}");
	}

	/// <summary>
	/// Background task that initiates battery cooling every five seconds.
	/// </summary>
	public void BackgroundCooling()
	{
		while (true)
		{
			Thread.Sleep(5000);
			CoolBattery();
		}
	}

	/// <summary>
	/// Function that handles battery status and usability when overheated.
	/// </summary>
	public void ShutoffBattery()
	{
		mState.BatteryStatus = BatteryStatus.Overheated;
		mLog.Info("Battery has overheated. It's unusable at this time.");
		while (mState.BatteryTemperature > BatterySpecs.MaxBatteryTemperature / 2) { } // Do nothing
		mLog.Info("Battery cooled off sufficiently. It's open for use again.");
		mState.BatteryStatus = BatteryStatus.Active;
	}

	/// <summary>
	/// Get next unique ID from the server. Is used by users / chargers to acquire IDs.
	/// </summary>
	/// <returns> Unique ID. </returns>
	public int GetUniqueId()
	{
		lock (mState.AccessLock)
		{
			mState.LastUniqueId += 1;
			return mState.LastUniqueId;
		}
	}

	/// <summary>
	/// Get current battery status.
	/// </summary>
	/// <returns> Current battery status. </returns>				
	public BatteryStatus GetBatteryStatus()
	{
		lock (mState.AccessLock)
		{
			return mState.BatteryStatus;
		}
	}

	/// <summary>
	/// Handles battery energy adjustment by users and chargers.
	/// </summary>
	/// <param name="Client"></param>
	/// <returns> Energy adjustment result. </returns>
	public bool AdjustEnergyLevel(ClientDesc Client)
	{
		var rnd = new Random();

		mLog.Info($"Client {Client.ClientType} {Client.ClientId} trying to adjust battery energy levels.");
		if (mState.BatteryStatus == BatteryStatus.Overheated)
		{
			mLog.Info("Battery is overheated. Cannot adjust energy level.");
			return false;
		}

		var amount = rnd.Next(1, 10);
		if (Client.ClientType == ClientType.User)
		{
			lock (mState.AccessLock)
			{
				if (mState.BatteryEnergy > BatterySpecs.MinBatteryEnergy)
				{
					if (mState.BatteryEnergy - amount <= BatterySpecs.MinBatteryEnergy)
					{
						mState.BatteryEnergy = BatterySpecs.MinBatteryEnergy;
					}
					else
					{
						mState.BatteryEnergy -= amount;
					}

					mState.BatteryTemperature += amount;
					mLog.Info($"Battery energy adjusted to {mState.BatteryEnergy} by {Client.ClientType} {Client.ClientId}");
					mLog.Info($"Battery temperature rose to {mState.BatteryTemperature}");

					if (mState.BatteryTemperature > BatterySpecs.MaxBatteryTemperature)
					{
						ShutoffBattery();
					}
					return true;
				}
				else
				{
					mLog.Info("Unable to lower energy level as it has insufficient energy.");
					return false;
				}
			}

		}
		else
		{
			if (mState.BatteryEnergy < BatterySpecs.MaxBatteryEnergy)
			{
				if (mState.BatteryEnergy + amount >= BatterySpecs.MaxBatteryEnergy)
				{
					mState.BatteryEnergy = BatterySpecs.MaxBatteryEnergy;
				}
				else
				{
					mState.BatteryEnergy += amount;
				}

				mState.BatteryTemperature += amount;
				mLog.Info($"Battery energy adjusted to {mState.BatteryEnergy} by {Client.ClientType} {Client.ClientId}");
				mLog.Info($"Battery temperature rose to {mState.BatteryTemperature}");

				if (mState.BatteryTemperature > BatterySpecs.MaxBatteryTemperature)
				{
					ShutoffBattery();
				}
				return true;
			}
			else
			{
				mLog.Info("Unable highten energy level as it has maximum energy.");
				return false;
			}
		}
	}
}