using Godot;
using System.Collections.Generic;

/// <summary>
/// 层级管理器，负责在细节视角和战略视角之间进行逻辑分发和状态同步
/// </summary>
[GlobalClass]
public partial class LayerManagerNode : Node3D
{
    private DetailLayerNode detailLayer = null!;
    private StrategyLayerNode strategyLayer = null!;
    private List<LayerNode> layers = new();

    /// <summary>从细节视图切换到战略视图的缩放高度阈值</summary>
    public float StrategyViewThreshold = 85.0f;

    public override void _Ready()
    {
        setupLayers();
    }

    // 内部初始化各显示层
    private void setupLayers()
    {
        detailLayer = new DetailLayerNode();
        detailLayer.Name = "DetailLayer";
        AddChild(detailLayer);
        layers.Add(detailLayer);
        
        strategyLayer = new StrategyLayerNode();
        strategyLayer.Name = "StrategyLayer";
        AddChild(strategyLayer);
        layers.Add(strategyLayer);
        
        // 初始状态：只显示细节层
        detailLayer.SetAlpha(1.0f);
        strategyLayer.SetAlpha(0.0f);
    }

    /// <summary>
    /// 当基础地图数据发生变化时，同步更新所有层
    /// </summary>
    /// <param name="ground">地形数据模型</param>
    public void UpdateMapData(Ground ground)
    {
        foreach (LayerNode layer in layers)
        {
            if (layer.Visible)
            {
                layer.UpdateLayer(ground);
            }
        }
    }

    /// <summary>
    /// 增量更新指定地格的各层表现
    /// </summary>
    /// <param name="cells">发生变化的地格坐标集合</param>
    /// <param name="ground">地形数据模型</param>
    public void UpdateCells(Godot.Collections.Array<Vector2I> cells, Ground ground)
    {
        foreach (LayerNode layer in layers)
        {
            if (layer.Visible)
            {
                layer.UpdateCells(cells, ground);
            }
        }
    }

    /// <summary>
    /// 根据当前的缩放深度，动态调整各层的可见度（Alpha）
    /// </summary>
    /// <param name="zoomValue">当前缩放值</param>
    public void HandleZoom(float zoomValue)
    {
        if (zoomValue > StrategyViewThreshold)
        {
            detailLayer.SetAlpha(0.0f);
            strategyLayer.SetAlpha(1.0f);
        }
        else
        {
            detailLayer.SetAlpha(1.0f);
            strategyLayer.SetAlpha(0.0f);
        }
    }
}
