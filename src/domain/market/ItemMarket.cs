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
	/// 无参构造函数，供业务创建与反持久化调用。
	/// </summary>
	public ItemMarket() { }

	/// <summary>
	/// 提交买单到买盘订单簿。
	/// </summary>
	/// <param name="price">买方愿意支付的价格。</param>
	/// <param name="quantity">买入数量。</param>
	/// <param name="agentId">下单代理标识。</param>
	public void PlaceBuyOrder(float price, int quantity, int agentId)
	{
		buyBook.AddOrder(new Order(agentId, price, quantity));
	}

	/// <summary>
	/// 提交卖单到卖盘订单簿。
	/// </summary>
	/// <param name="price">卖方期望成交价格。</param>
	/// <param name="quantity">卖出数量。</param>
	/// <param name="agentId">下单代理标识。</param>
	public void PlaceSellOrder(float price, int quantity, int agentId)
	{
		sellBook.AddOrder(new Order(agentId, price, quantity));
	}

	/// <summary>
	/// 清理指定代理在买卖两侧的挂单。
	/// </summary>
	/// <param name="agent">待清理挂单的代理。</param>
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
	/// 按买盘优先级（价格高优先、同价时间早优先）返回挂单快照。
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
	/// 按卖盘优先级（价格低优先、同价时间早优先）返回挂单快照。
	/// </summary>
	public IReadOnlyList<(float price, int quantity)> GetSellOrderBookSnapshot()
	{
		return sellBook.Orders
			.OrderBy(order => order.Price)
			.ThenBy(order => order.Timestamp)
			.Select(order => (order.Price, order.Quantity))
			.ToList();
	}

	// 按价格优先、时间优先规则撮合一笔成交，并回写订单剩余数量。
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
