using Godot;

/// <summary>
/// 建筑物的几何体渲染器，负责其 3D 模型的表现
/// </summary>
[GlobalClass]
public partial class BuildingMesh : MeshInstance3D
{
	// 网格实例独占材质。
	private StandardMaterial3D material = null!;
	// 建筑顶部标签。
	private Label3D label = null!;

	/// <summary>
	/// 初始化网格渲染依赖。
	/// </summary>
	public override void _Ready()
	{
		// 材质已经在 .tscn 中配置好，作为 SurfaceOverrideMaterial(0)
		// 确保材质是唯一的，否则所有建筑会呈现同一种颜色
		StandardMaterial3D original = (StandardMaterial3D)GetSurfaceOverrideMaterial(0);
		material = (StandardMaterial3D)original.Duplicate();
		SetSurfaceOverrideMaterial(0, material);

		label = GetNode<Label3D>("Label3D");
	}

	/// <summary>
	/// 设置显示的文字
	/// </summary>
	/// <param name="text">文字内容</param>
	public void UpdateText(string text)
	{
		label.Text = text;
	}

	/// <summary>
	/// 更新网格的表现颜色
	/// </summary>
	public void UpdateColor(Color color)
	{
		material.AlbedoColor = color;
	}

	/// <summary>
	/// 更新高亮状态 (如：选中、市场覆盖等)
	/// </summary>
	/// <param name="selected">是否为选中状态</param>
	/// <param name="isMarketCovered">是否被市场覆盖</param>
	/// <param name="buildingColor">基础颜色</param>
	public void UpdateHighlight(bool selected, bool isMarketCovered, Color buildingColor)
	{
		Color finalColor = buildingColor;
		if (selected)
		{
			// 混合黄色并提亮
			finalColor = finalColor.Lerp(Colors.Yellow, 0.5f) * 1.5f;
		}
		else if (isMarketCovered)
		{
			// 混合三文鱼红并提亮
			finalColor = finalColor.Lerp(Colors.Salmon, 0.65f) * 1.25f;
		}

		material.AlbedoColor = finalColor;
	}
}
