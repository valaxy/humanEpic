using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 商品市场，负责撮合交易。
/// </summary>
[Persistable]
public class ItemMarket
{
	// 买盘订单簿集合。
	[PersistField]
	private BuySide buyBook = new BuySide();

	// 卖盘订单簿集合。
	[PersistField]
	private SellSide sellBook = new SellSide();

	/// <summary>
	/// 当前买单总量。
	/// </summary>
	public int BuyQuantity => buyBook.TotalQuantity;

	/// <summary>
	/// 当前卖单总量。
	/// </summary>
	public int SellQuantity => sellBook.TotalQuantity;

	/// <summary>
	/// 当前买单快照。
	/// </summary>
	public IReadOnlyList<Order> BuyOrders => buyBook.Orders;

	/// <summary>
	/// 当前卖单快照。
	/// </summary>
	public IReadOnlyList<Order> SellOrders => sellBook.Orders;


	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private ItemMarket() { }

	public void PlaceBuyOrder(float price, int quantity, int agentId)
	{
		buyBook.AddOrder(new Order(agentId, price, quantity));
	}

	public void PlaceSellOrder(float price, int quantity, int agentId)
	{
		sellBook.AddOrder(new Order(agentId, price, quantity));
	}

	public void ClearAgentOrders(IAgent agent)
	{
		buyBook.ClearAgentOrders(agent);
		sellBook.ClearAgentOrders(agent);
	}

	/// <summary>
	/// 如果有订单可以成交就直接成交。
	/// </summary>
	/// <returns>成交记录列表</returns>
	public List<Trade> TradeAll()
	{
		List<Trade> trades = [];

		while (true)
		{
			if (sellBook.OrderCount == 0) { break; }
			if (buyBook.OrderCount == 0) { break; }
			if (sellBook.PeekTopOrder().Price > buyBook.PeekTopOrder().Price) { break; }

			Order topSellOrder = sellBook.PopTopOrder();
			Order topBuyOrder = buyBook.PopTopOrder();
			Trade trade = closeOrder(topSellOrder, topBuyOrder);
			trades.Add(trade);

			if (topSellOrder.Quantity > 0)
			{
				sellBook.AddOrder(topSellOrder);
			}

			if (topBuyOrder.Quantity > 0)
			{
				buyBook.AddOrder(topBuyOrder);
			}
		}

		return trades;
	}

	/// <summary>
	/// 按买盘价格排序的挂单快照。
	/// </summary>
	public IReadOnlyList<(float price, int quantity)> GetBuyOrderBookSnapshot()
	{
		return buyBook.Orders
			.OrderByDescending(order => order.Price)
			.ThenBy(order => order.Timestamp)
			.Select(order => (order.Price, order.Quantity))
			.ToList();
	}

	/// <summary>
	/// 按卖盘价格排序的挂单快照。
	/// </summary>
	public IReadOnlyList<(float price, int quantity)> GetSellOrderBookSnapshot()
	{
		return sellBook.Orders
			.OrderBy(order => order.Price)
			.ThenBy(order => order.Timestamp)
			.Select(order => (order.Price, order.Quantity))
			.ToList();
	}

	// 撮合交易。
	private Trade closeOrder(Order sellOrder, Order buyOrder)
	{
		Debug.Assert(sellOrder.Price <= buyOrder.Price, "卖盘价格必须小于等于买盘价格");

		int closeNum = Math.Min(sellOrder.Quantity, buyOrder.Quantity);
		float closePrice = sellOrder.Timestamp < buyOrder.Timestamp ? sellOrder.Price : buyOrder.Price;
		sellOrder.Fill(closeNum);
		buyOrder.Fill(closeNum);
		return new Trade(buyOrder.AgentId, sellOrder.AgentId, closePrice, closeNum);
	}
}
