using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SPTarkov.Server.Core.Services;

public class RepairDetails
{
	[JsonPropertyName("repairCost")]
	public virtual double? RepairCost { get; set; }

	[JsonPropertyName("repairPoints")]
	public virtual double? RepairPoints { get; set; }

	[JsonPropertyName("repairedItem")]
	public virtual Item? RepairedItem { get; set; }

	[JsonPropertyName("repairedItemIsArmor")]
	public virtual bool? RepairedItemIsArmor { get; set; }

	[JsonPropertyName("repairAmount")]
	public virtual double? RepairAmount { get; set; }

	[JsonPropertyName("repairedByKit")]
	public virtual bool? RepairedByKit { get; set; }
}
