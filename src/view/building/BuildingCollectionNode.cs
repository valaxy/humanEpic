using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建筑集合节点，负责所有建筑的 3D 渲染与选中高亮。
/// </summary>
[GlobalClass]
public partial class BuildingCollectionNode : Node3D
{
	// 世界模型。
	private GameWorld world = null!;
	// 当前选中的建筑。
	private Building? selectedBuilding = null;
	// 建筑位置到视图节点映射。
	private readonly Dictionary<Vector2I, BuildingNode> buildingNodes = new();

	/// <summary>
	/// 设置依赖并初始化
	/// </summary>
	/// <param name="world">游戏世界实例</param>
	public void Setup(GameWorld world)
	{
		this.world = world;

		world.Buildings.Added += _ => UpdateBuildingVisuals();
		world.Buildings.Removed += _ => UpdateBuildingVisuals();

		UpdateBuildingVisuals();
	}

	/// <summary>
	/// 刷新所有建筑的可视化展示
	/// </summary>
	public void RefreshVisuals()
	{
		UpdateBuildingVisuals();
	}

	/// <summary>
	/// 显示建筑选中效果。
	/// </summary>
	/// <param name="building">选中的建筑</param>
	public void ShowBuildingVirtual(Building building)
	{
		selectedBuilding = building;

		UpdateBuildingVisuals();
	}

	/// <summary>
	/// 隐藏建筑预览
	/// </summary>
	public void HideBuildingInfo()
	{
		selectedBuilding = null;
		UpdateBuildingVisuals();
	}

	/// <summary>
	/// 清理所有范围渲染
	/// </summary>
	public void ClearRangeRender()
	{
		// 当前版本无范围渲染，保留该接口用于兼容旧调用点。
	}

	/// <summary>
	/// 更新建筑表现
	/// </summary>
	public void UpdateBuildingVisuals()
	{
		if (world == null)
		{
			return;
		}

		renderBuildings(world.Buildings.GetAll().ToList());
	}

	// 渲染全部建筑并清理失效节点。
	private void renderBuildings(List<Building> buildings)
	{
		HashSet<Vector2I> currentPositions = buildings
			.Select(building => building.Collision.Center)
			.ToHashSet();

		buildings.ForEach(building =>
		{
			Vector2I pos = building.Collision.Center;
			float worldY = getYAtGrid(pos);
			Vector3 worldPos = world.Ground.GridToWorld(pos, worldY);
			updateBuildingNode(building, pos, worldPos, building == selectedBuilding);
		});

		cleanupInvalidNodes(currentPositions);
	}

	private float getYAtGrid(Vector2I gridPos)
	{
		return YConfig.PlainY;
	}

	// 更新或创建建筑节点。
	private void updateBuildingNode(Building building, Vector2I pos, Vector3 worldPos, bool isSelected)
	{
		if (!buildingNodes.TryGetValue(pos, out BuildingNode? node))
		{
			node = new BuildingNode();
			AddChild(node);
			buildingNodes[pos] = node;
		}

		node.Position = worldPos;
		node.Update(building, isSelected);
	}

	// 移除已经不在集合中的节点。
	private void cleanupInvalidNodes(HashSet<Vector2I> currentPositions)
	{
		List<Vector2I> positionsToRemove = buildingNodes
			.Where(pair => !currentPositions.Contains(pair.Key))
			.Select(pair => pair.Key)
			.ToList();

		positionsToRemove.ForEach(pos =>
		{
			buildingNodes[pos].QueueFree();
			buildingNodes.Remove(pos);
		});
	}
}
