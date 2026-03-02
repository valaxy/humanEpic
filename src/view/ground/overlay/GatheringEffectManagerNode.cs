using Godot;
using System.Collections.Generic;

/// <summary>
/// 管理一个用于反馈地格采集状态的视觉反馈层。它接收领域层的采集信号并展示闪烁效果。
/// </summary>
public partial class GatheringEffectManagerNode : Node3D
{
	private Dictionary<Vector2I, GatheringEffectNode> activeEffects = new();
	private Ground? ground;

	/// <summary>
	/// 初始化地理显示数据支持
	/// </summary>
	public void Setup(Ground ground)
	{
		this.ground = ground;
		EventHub.Instance().GridGatheringTriggered += OnGridGatheringTriggered;
	}

	/// <summary>
	/// 处理地格资源被采集的回调，创建或刷新闪烁节点
	/// </summary>
	private void OnGridGatheringTriggered(Vector2I gridPos)
	{
		if (ground == null) return;

		// 如果此地格已经有效果正在执行，直接重置其周期，防止节点重复创建消耗性能
		if (activeEffects.TryGetValue(gridPos, out GatheringEffectNode? existingEffect))
		{
			if (IsInstanceValid(existingEffect))
			{
				existingEffect.Reset();
				return;
			}
			activeEffects.Remove(gridPos);
		}

		// 创建新的视觉反馈实例
		GatheringEffectNode newEffect = new GatheringEffectNode();
		AddChild(newEffect);
		
		// 设定深度值，确保效果能够盖在地表上面展现
		Vector3 worldPos = ground.GridToWorld(gridPos, YConfig.SelectionY); 
		newEffect.GlobalPosition = worldPos;
		
		activeEffects[gridPos] = newEffect;
		
		// 连接节点移除信号，实时同步字典状态
		newEffect.TreeExiting += () => {
			if (activeEffects.ContainsKey(gridPos) && activeEffects[gridPos] == newEffect)
			{
				activeEffects.Remove(gridPos);
			}
		};
	}

	public override void _ExitTree()
	{
		// 程序退出或节点移除时，清理事件监听，防止内存泄露
		EventHub.Instance().GridGatheringTriggered -= OnGridGatheringTriggered;
	}
}
