namespace Services;

/// <summary>
/// Client descriptor.
/// </summary>
public class ClientDesc
{
	/// <summary>
	/// Client ID
	/// </summary>
	/// <value></value>
	public int ClientId { get; set; }

	/// <summary>
	/// Client type
	/// </summary>
	/// <value> User, Charger </value>
	public ClientType ClientType { get; set; }
}

/// <summary>
/// Type of client - User or Charger.
/// </summary>
public enum ClientType : int
{
	User,
	Charger
}

/// <summary>
/// Battery status - Active or Overheated.
/// </summary>
public enum BatteryStatus : int
{
	Active,
	Overheated
}

/// <summary>
/// Service contract.
/// </summary>
public interface IBatteryService
{
	int GetUniqueId();
	BatteryStatus GetBatteryStatus();
	bool AdjustEnergyLevel(ClientDesc Client);

}