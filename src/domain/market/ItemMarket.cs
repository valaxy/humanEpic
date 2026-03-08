using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 商品市场，负责撮合交易。
/// </summary>
public abstract class ItemMarket
{
	// 买盘订单簿集合。
	private readonly BuySide buyBook = new BuySide();

	// 卖盘订单簿集合。
	private readonly SellSide sellBook = new SellSide();


	/// <summary>
	/// 如果有订单可以成交就直接成交
	/// </summary>
	/// <returns></returns>
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
