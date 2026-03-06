using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 建筑集合节点，管理所有建筑的 3D 渲染与选中高亮等。
/// </summary>
[GlobalClass]
public partial class BuildingCollectionView : Node3D
{
	// 世界模型。
	private GameWorld world = null!;

	// 当前选中的建筑。
	private Building? selectedBuilding = null;

	// 建筑位置到视图节点映射。
	private readonly Dictionary<Vector2I, BuildingView> buildingNodes = new();


	/// <summary>
	/// 当任意建筑几何体被点击时发出。
	/// </summary>
	[Signal]
	public delegate void BuildingClickedEventHandler(Vector2I cellPos);

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
	/// 取消选中当前建筑。
	/// </summary>
	public void Unselect()
	{
		selectedBuilding = null;
		UpdateBuildingVisuals();
	}


	/// <summary>
	/// 渲染全部建筑并清理失效节点。
	/// </summary>
	public void UpdateBuildingVisuals()
	{
		List<Building> buildings = world.Buildings.GetAll().ToList();
		HashSet<Vector2I> currentPositions = buildings
			.Select(building => building.Collision.Center)
			.ToHashSet();

		buildings.ForEach(building =>
		{
			Vector2I pos = building.Collision.Center;
			float worldY = YConfig.PlainY;
			Vector3 worldPos = world.Ground.GridToWorld(pos, worldY);
			updateBuildingView(building, pos, worldPos, building == selectedBuilding);
		});

		cleanupInvalidViews(currentPositions);
	}

	// 更新或创建建筑节点。
	private void updateBuildingView(Building building, Vector2I pos, Vector3 worldPos, bool isSelected)
	{
		if (!buildingNodes.TryGetValue(pos, out BuildingView? node))
		{
			node = new BuildingView();
			node.BuildingClicked += onBuildingNodeClicked;
			AddChild(node);
			buildingNodes[pos] = node;
		}

		node.Position = worldPos;
		node.Update(building, isSelected);
	}

	// 移除已经不在集合中的节点。
	// TOOD 为什么要出现要移除的流程来着？
	private void cleanupInvalidViews(HashSet<Vector2I> currentPositions)
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

	// 将单建筑节点点击信号统一转发给外部系统。
	private void onBuildingNodeClicked(Vector2I cellPos)
	{
		Building building = world.Buildings.Get(cellPos);
		selectedBuilding = building;
		UpdateBuildingVisuals();
		EmitSignal(SignalName.BuildingClicked, cellPos);

		Debug.Print($"点击了建筑，位置：{cellPos}");
	}
}