using Godot;
using EchoesOfCommand.Enums;

namespace EchoesOfCommand.Interfaces;

public interface IOrderSystem
{
    (string OrderId, float EstimatedArrivalTime) IssueOrder(
        int targetShipId,
        OrderType orderType,
        Vector3? position = null,
        int? targetId = null);
}
