using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Inventory;

namespace SPTarkov.Server.Core.Models.Eft.Repair;

public record TraderRepairActionDataRequest : InventoryBaseActionRequestData
{
	[JsonPropertyName("tid")]
	public virtual MongoId TraderId { get; set; }

	[JsonPropertyName("repairItems")]
	public virtual List<RepairItem>? RepairItems { get; set; }

	[JsonExtensionData]
	public new Dictionary<string, object> ExtensionData
	{
		get
		{
			return _extensionData;
		}
		set
		{
			_extensionData = value;
		}
	}

	[JsonIgnore]
	private readonly Dictionary<string, object> _extensionData = new Dictionary<string, object>();

	[CompilerGenerated]
	protected override bool PrintMembers(StringBuilder builder)
	{
		if (base.PrintMembers(builder))
		{
			builder.Append(", ");
		}
		builder.Append("TraderId = ");
		builder.Append(TraderId.ToString());
		builder.Append(", RepairItems = ");
		builder.Append(RepairItems);
		return true;
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (base.GetHashCode() * -1521134295 + EqualityComparer<MongoId>.Default.GetHashCode(TraderId)) * -1521134295 + EqualityComparer<List<RepairItem>>.Default.GetHashCode(RepairItems);
	}

	[CompilerGenerated]
	public virtual bool Equals(TraderRepairActionDataRequest? other)
	{
		if ((object)this != other)
		{
			if (base.Equals(other) && EqualityComparer<MongoId>.Default.Equals(TraderId, other.TraderId))
			{
				return EqualityComparer<List<RepairItem>>.Default.Equals(RepairItems, other.RepairItems);
			}
			return false;
		}
		return true;
	}

	[CompilerGenerated]
	protected TraderRepairActionDataRequest(TraderRepairActionDataRequest original)
	{
		_extensionData = new Dictionary<string, object>();
		base..ctor(original);
		TraderId = original.TraderId;
		RepairItems = original.RepairItems;
	}

	public TraderRepairActionDataRequest()
	{
	}
}
