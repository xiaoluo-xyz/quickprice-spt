using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Inventory;

namespace SPTarkov.Server.Core.Models.Eft.Repair;

public record RepairActionDataRequest : InventoryBaseActionRequestData
{
	[JsonPropertyName("repairKitsInfo")]
	public virtual List<RepairKitsInfo>? RepairKitsInfo { get; set; }

	/// <summary>
	///     item to repair
	/// </summary>
	[JsonPropertyName("target")]
	public virtual MongoId? Target { get; set; }

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
		builder.Append("RepairKitsInfo = ");
		builder.Append(RepairKitsInfo);
		builder.Append(", Target = ");
		builder.Append(Target.ToString());
		return true;
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (base.GetHashCode() * -1521134295 + EqualityComparer<List<SPTarkov.Server.Core.Models.Eft.Repair.RepairKitsInfo>>.Default.GetHashCode(RepairKitsInfo)) * -1521134295 + EqualityComparer<MongoId?>.Default.GetHashCode(Target);
	}

	[CompilerGenerated]
	public virtual bool Equals(RepairActionDataRequest? other)
	{
		if ((object)this != other)
		{
			if (base.Equals(other) && EqualityComparer<List<SPTarkov.Server.Core.Models.Eft.Repair.RepairKitsInfo>>.Default.Equals(RepairKitsInfo, other.RepairKitsInfo))
			{
				return EqualityComparer<MongoId?>.Default.Equals(Target, other.Target);
			}
			return false;
		}
		return true;
	}

	[CompilerGenerated]
	protected RepairActionDataRequest(RepairActionDataRequest original)
	{
		_extensionData = new Dictionary<string, object>();
		base..ctor(original);
		RepairKitsInfo = original.RepairKitsInfo;
		Target = original.Target;
	}

	public RepairActionDataRequest()
	{
	}
}
