using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 订单簿单边结构。
/// </summary>
public abstract class Side
{
    // 价格优先、时间优先的有序订单集。
    private readonly SortedSet<Order> orders;

    // 每个Agent只能登记一次订单
    private readonly Dictionary<int, bool> hasOrders = new();

    /// <summary>
    /// 订单数量。
    /// </summary>
    public int OrderCount => orders.Count;

    /// <summary>
    /// 初始化单边订单簿。
    /// </summary>
    protected Side()
    {
        orders = Init();
    }

    /// <summary>
    /// 初始化订单排序规则。
    /// </summary>
    protected abstract SortedSet<Order> Init();



    /// <summary>
    /// 添加订单。
    /// </summary>
    public void AddOrder(Order order)
    {
        Debug.Assert(!hasOrders.ContainsKey(order.Agent.AgentId), "同一个Agent只能登记一个订单。");

        orders.Add(order);
        hasOrders[order.Agent.AgentId] = true;
    }



    /// <summary>
    /// 读取顶档订单。
    /// </summary>
    public Order PeekTopOrder()
    {
        return orders.Min!;
    }

    /// <summary>
    /// 弹出顶档订单。
    /// </summary>
    public Order PopTopOrder()
    {
        Order order = orders.Min!;
        orders.Remove(order);
        return order;
    }


    /// <summary>
    /// 按订单号删除订单。
    /// </summary>
    public void ClearAgentOrders(IAgent agent)
    {
        if (!hasOrders.ContainsKey(agent.AgentId)) { return; }

        Order? orderToRemove = orders.FirstOrDefault(order => order.Agent.AgentId == agent.AgentId);
        if (orderToRemove != null)
        {
            orders.Remove(orderToRemove);
            hasOrders.Remove(agent.AgentId);
        }
    }




    // /// <summary>
    // /// 根据订单号查询订单。
    // /// </summary>
    // public bool TryGetOrder(long orderId, out Order? order)
    // {
    //     bool found = ordersById.TryGetValue(orderId, out Order? foundOrder);
    //     order = foundOrder;
    //     return found;
    // }

    // /// <summary>
    // /// 清空全部订单。
    // /// </summary>
    // public IReadOnlyCollection<long> ClearAllOrders()
    // {
    //     List<long> removedIds = ordersById.Keys.ToList();
    //     orders.Clear();
    //     ordersById.Clear();
    //     return removedIds;
    // }



    //protected abstract Order consume(Order[] orders);


    //public void UpdatePrice(Order order, float price)
    //{
    //    order.Price = price;
    //}

    //public void UpdateNum(Order order, int num)
    //{
    //    order.Num = num;
    //}

    //public void IncNum(Order order, int incNum)
    //{
    //    order.Num += incNum;
    //}

    //public Order TakeOrder()
    //{
    //    if (IsEmpty()) { throw new InvalidOperationException(); }

    //    var sortedOrders = orders
    //        .OrderBy(p => subjectivePrice(p.Price))
    //        .ToArray();

    //    Order takeOrder = consume(sortedOrders);

    //    takeOrder.Num -= 1;
    //    if (takeOrder.Num == 0)
    //    {
    //        orders.Remove(takeOrder); // TODO 用链表实现更快
    //    }

    //    return takeOrder;
    //}


    //private float subjectivePrice(float price)
    //{
    //    Random random = new Random();
    //    float amendPrice = price * (1 + generateNormalRandom(random, 0, 0.2)); // TODO 0.2系数需要调整
    //    return amendPrice;
    //}


    //private float generateNormalRandom(Random random, double mean, double standardDeviation)
    //{
    //    // 使用Box-Muller变换生成正态分布随机数
    //    // Box-Muller变换公式: Z = sqrt(-2*ln(U)) * cos(2*pi*V)
    //    // 其中U和V是[0,1)之间的均匀分布随机数

    //    double u1 = random.NextDouble(); // 第一个均匀分布随机数
    //    double u2 = random.NextDouble(); // 第二个均匀分布随机数

    //    // 应用Box-Muller变换
    //    double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

    //    // 根据均值和标准差调整结果
    //    return (float)(mean + standardDeviation * z0);
    //}


    //public void Print()
    //{
    //    var sortedOrders = orders.OrderBy(o => o.Price);

    //    foreach (Order order in sortedOrders)
    //    {
    //        Console.WriteLine($"{order.Price:F2} × {order.Num}, {order.Agent}");
    //    }
    //}
}


