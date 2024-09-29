namespace Servers;

using NLog;
using Services;

/// <summary>
/// Service
/// </summary>
public class BatteryService : IBatteryService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly BatteryLogic mLogic = new BatteryLogic();

	/// <summary>
	/// Get next unique ID from the server. Is used by users / chargers to acquire IDs.
	/// </summary>
	/// <returns> Unique ID. </returns>
	public int GetUniqueId() 
	{
		return mLogic.GetUniqueId();
	}

	/// <summary>
	/// Get current battery status.
	/// </summary>
	/// <returns> Current battery status. </returns>		
    public BatteryStatus GetBatteryStatus()
    {
        return mLogic.GetBatteryStatus();
    }

	/// <summary>
	/// Handles battery energy adjustment by users and chargers.
	/// </summary>
	/// <param name="Client"></param>
	/// <returns> Energy adjustment result. </returns>
    public bool AdjustEnergyLevel(ClientDesc Client)
    {
        return mLogic.AdjustEnergyLevel(Client);
    }

}