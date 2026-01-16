using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace SPTarkov.Server.Core.Models.Eft.Repair;

public record RepairItem
{
	[JsonPropertyName("_id")]
	public virtual MongoId Id { get; set; }

	[JsonPropertyName("count")]
	public virtual double? Count { get; set; }

	[JsonExtensionData]
	public Dictionary<string, object> ExtensionData
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
	protected virtual bool PrintMembers(StringBuilder builder)
	{
		RuntimeHelpers.EnsureSufficientExecutionStack();
		builder.Append("Id = ");
		builder.Append(Id.ToString());
		builder.Append(", Count = ");
		builder.Append(Count.ToString());
		return true;
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<MongoId>.Default.GetHashCode(Id)) * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Count);
	}

	[CompilerGenerated]
	public virtual bool Equals(RepairItem? other)
	{
		if ((object)this != other)
		{
			if ((object)other != null && EqualityContract == other.EqualityContract && EqualityComparer<MongoId>.Default.Equals(Id, other.Id))
			{
				return EqualityComparer<double?>.Default.Equals(Count, other.Count);
			}
			return false;
		}
		return true;
	}

	[CompilerGenerated]
	protected RepairItem(RepairItem original)
	{
		_extensionData = new Dictionary<string, object>();
		base..ctor();
		Id = original.Id;
		Count = original.Count;
	}

	public RepairItem()
	{
	}
}
