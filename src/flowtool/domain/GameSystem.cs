using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 整个游戏演化的复杂系统，负责管理多个 MetricScope。
/// </summary>
public sealed class GameSystem
{
	// 作用域集合。
	private readonly Dictionary<string, MetricScope> scopes;

	/// <summary>
	/// 当前作用域列表。
	/// </summary>
	public IDictionary<string, MetricScope> Scopes => scopes;


	/// <summary>
	/// 创建仅包含拓扑数据的系统对象。
	/// </summary>
	public GameSystem(IEnumerable<MetricScope> scopes)
	{
		this.scopes = scopes.ToDictionary(scope => scope.Name, StringComparer.Ordinal);
	}
}
