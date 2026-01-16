using System.Collections.Generic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Repair;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;

namespace SPTarkov.Server.Core.Controllers;

[Injectable(InjectionType.Scoped, null, int.MaxValue)]
public class RepairController(EventOutputHolder eventOutputHolder, RepairService repairService)
{
	/// <summary>
	///     Handle TraderRepair event
	///     Repair with trader
	/// </summary>
	/// <param name="sessionID">session id</param>
	/// <param name="request">endpoint request data</param>
	/// <param name="pmcData">player profile</param>
	/// <returns>ItemEventRouterResponse</returns>
	public virtual ItemEventRouterResponse TraderRepair(MongoId sessionID, TraderRepairActionDataRequest request, PmcData pmcData)
	{
		ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionID);
		foreach (RepairItem repairItem in request.RepairItems)
		{
			RepairDetails repairDetails = repairService.RepairItemByTrader(sessionID, pmcData, repairItem, request.TraderId);
			repairService.PayForRepair(sessionID, pmcData, repairItem.Id, repairDetails.RepairCost.Value, request.TraderId, output);
			List<Warning>? warnings = output.Warnings;
			if (warnings != null && warnings.Count > 0)
			{
				return output;
			}
			output.ProfileChanges[sessionID].Items.ChangedItems.Add(repairDetails.RepairedItem);
			repairService.AddRepairSkillPoints(sessionID, repairDetails, pmcData);
		}
		return output;
	}

	/// <summary>
	///     Handle Repair event
	///     Repair with repair kit
	/// </summary>
	/// <param name="sessionId">session id</param>
	/// <param name="body">endpoint request data</param>
	/// <param name="pmcData">player profile</param>
	/// <returns>ItemEventRouterResponse</returns>
	public virtual ItemEventRouterResponse RepairWithKit(MongoId sessionId, RepairActionDataRequest body, PmcData pmcData)
	{
		ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);
		RepairDetails repairDetails = repairService.RepairItemByKit(sessionId, pmcData, body.RepairKitsInfo, body.Target.Value, output);
		repairService.AddBuffToItem(repairDetails, pmcData);
		output.ProfileChanges[sessionId].Items.ChangedItems.Add(repairDetails.RepairedItem);
		repairService.AddRepairSkillPoints(sessionId, repairDetails, pmcData);
		return output;
	}
}
