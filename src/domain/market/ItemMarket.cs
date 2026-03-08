using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 商品市场，负责撮合交易。
/// </summary>
public class ItemMarket
{
	// 买盘订单簿集合。
	private readonly BuySide buyBook = new BuySide();

	// 卖盘订单簿集合。
	private readonly SellSide sellBook = new SellSide();

	public void PlaceBuyOrder(float price, int quantity, IAgent agent)
	{
		buyBook.AddOrder(new Order(agent, price, quantity));
	}

	public void PlaceSellOrder(float price, int quantity, IAgent agent)
	{
		sellBook.AddOrder(new Order(agent, price, quantity));
	}

	public void ClearAgentOrders(IAgent agent)
	{
		buyBook.ClearAgentOrders(agent);
		sellBook.ClearAgentOrders(agent);
	}


	/// <summary>
	/// 如果有订单可以成交就直接成交
	/// </summary>
	/// <returns>成交记录列表</returns>
	public List<Trade> TradeAll()
	{
		List<Trade> trades = new List<Trade>();

		while (true)
		{
			if (sellBook.OrderCount == 0) { break; }
			if (buyBook.OrderCount == 0) { break; }
			if (sellBook.PeekTopOrder().Price > buyBook.PeekTopOrder().Price) { break; }

			Order topSellOrder = sellBook.PopTopOrder();
			Order topBuyOrder = buyBook.PopTopOrder();
			trades.Add(closeOrder(topSellOrder, topBuyOrder));
		}

		return trades;
	}

	// 撮合交易
	private Trade closeOrder(Order sellOrder, Order buyOrder)
	{
		Debug.Assert(sellOrder.Price <= buyOrder.Price, "卖盘价格必须小于等于买盘价格");

		int closeNum = Math.Min(sellOrder.Quantity, buyOrder.Quantity);
		float closePrice = sellOrder.Timestamp < buyOrder.Timestamp ? sellOrder.Price : buyOrder.Price;
		sellOrder.Fill(closeNum);
		buyOrder.Fill(closeNum);
		return new Trade(buyOrder.Agent, sellOrder.Agent, closePrice, closeNum);
	}
}