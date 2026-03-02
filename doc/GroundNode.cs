using Godot;
using System.Collections.Generic;

/// <summary>
/// 处理地面对象的渲染与交互
/// </summary>
[GlobalClass]
public partial class GroundNode : Node3D
{
    /// <summary>
    /// 当地格被绘制（刷过）时发出
    /// </summary>
    [Signal] public delegate void CellsDrawnEventHandler(Godot.Collections.Array<Vector2I> cells);
    
    /// <summary>
    /// 当地图更新时发出
    /// </summary>
    [Signal] public delegate void MapUpdatedEventHandler();

    /// <summary>
    /// 当一次连续绘制动作（按下到抬起）完成时发出
    /// </summary>
    [Signal] public delegate void DrawCompletedEventHandler();

    /// <summary>3D相机引用</summary>
    public GameCamera Camera { get; set; } = null!;
 
    /// <summary>地面数据模型引用</summary>
    public Ground? Ground { get; set; }
    
    /// <summary>游戏世界引用</summary>
    public GameWorld? World { get; set; }

    /// <summary>层级管理器引用</summary>
    public LayerManagerNode? LayerManager { get; set; }

    // /// <summary>选中管理器引用</summary>
    // public GroundSelectionNode? SelectionNode { get; set; }

    // /// <summary>区域渲染组件</summary>
    // public GridAreaNode? GridAreaRender { get; set; }

    // /// <summary>交互管理组件</summary>
    public GroundInteractionNode InteractionNode { get; set; } = null!;

    // /// <summary>编辑绘制组件</summary>
    // public GroundEditorNode EditorNode { get; set; } = null!;

    // /// <summary>笔刷光标节点</summary>
    // public BrushCursorNode? BrushCursor { get; set; }

    /// <summary>采集视觉管理器</summary>
    public GatheringEffectManagerNode GatheringEffectNode { get; set; } = null!;

    /// <summary>交互是否处于活动状态</summary>
    public bool IsActive { get => InteractionNode.IsActive; set => InteractionNode.IsActive = value; }
    
    /// <summary>是否允许绘制</summary>
    public bool CanDraw { get => InteractionNode.CanDraw; set => InteractionNode.CanDraw = value; }
    
    /// <summary>是否正在绘制中</summary>
    public bool IsDrawing { get => InteractionNode.IsDrawing; set => InteractionNode.IsDrawing = value; }
    
    /// <summary>是否处于覆盖物编辑模式</summary>
    public bool IsOverlayMode { get => EditorNode.IsOverlayMode; set => EditorNode.IsOverlayMode = value; }
    
    /// <summary>是否处于建筑编辑模式</summary>
    public bool IsBuildingEditorActive { get => EditorNode.IsBuildingEditorActive; set => EditorNode.IsBuildingEditorActive = value; }

    /// <summary>当前选中的建筑类型标识符</summary>
    public string CurrentSelectedBuilding { get => EditorNode.CurrentSelectedBuilding; set => EditorNode.CurrentSelectedBuilding = value; }

    /// <summary>当前选中的国家ID</summary>
    public int CurrentSelectedCountryId { get => EditorNode.CurrentSelectedCountryId; set => EditorNode.CurrentSelectedCountryId = value; }
    
    /// <summary>当前选中的地表类型</summary>
    public SurfaceType.Enums SelectedSurface { get => EditorNode.SelectedSurface; set => EditorNode.SelectedSurface = value; }
    
    /// <summary>当前选中的覆盖物类型</summary>
    public OverlayType.Enums SelectedOverlay { get => EditorNode.SelectedOverlay; set => EditorNode.SelectedOverlay = value; }

    /// <summary>当前选中的地形高度类型</summary>
    public TerrainHeight.Enums SelectedHeight { get => EditorNode.SelectedHeight; set => EditorNode.SelectedHeight = value; }

    /// <summary>是否同步更新地图数据</summary>
    public bool IsUpdatingGround { get => EditorNode.IsUpdatingGround; set => EditorNode.IsUpdatingGround = value; }

    private static readonly PackedScene brushCursorNodeScene = GD.Load<PackedScene>("res://src/view/ground/selection/BrushCursorNode.tscn");

    public override void _Ready()
    {
        AddToGroup("geography_manager");
        
        // 初始化拆分出的逻辑节点
        InteractionNode = new GroundInteractionNode();
        AddChild(InteractionNode);

        EditorNode = new GroundEditorNode();
        AddChild(EditorNode);

        GatheringEffectNode = new GatheringEffectManagerNode();
        AddChild(GatheringEffectNode);

        InteractionNode.Setup(this);
        EditorNode.Setup(this);

        // 绑定信号
        this.CellsDrawn += onCellsDrawn;
        this.DrawCompleted += onDrawCompleted;
    }

    /// <summary>
    /// 初始化地理管理器逻辑
    /// </summary>
    public void Setup(GameCameraNode camera, GameWorld world, GroundSelectionNode selectionNode, LayerManagerNode? layerMgr = null)
    {
        this.Camera = camera;
        this.World = world;
        this.SelectionNode = selectionNode;
        this.Ground = world.Ground;
        this.LayerManager = layerMgr;

        if (BrushCursor == null)
        {
            BrushCursor = brushCursorNodeScene.Instantiate<BrushCursorNode>();
            AddChild(BrushCursor);
        }

        if (GridAreaRender == null)
        {
            GridAreaRender = new GridAreaNode();
            AddChild(GridAreaRender);
        }

        GatheringEffectNode.Setup(world.Ground);
    }

    /// <summary>
    /// 设置管理器是否处于活动状态
    /// </summary>
    public void SetActive(bool active)
    {
        IsActive = active;
        if (!active)
        {
            SelectionNode?.ClearSelection();
        }
    }

    /// <summary>
    /// 设置是否可以绘制
    /// </summary>
    public void SetCanDraw(bool can)
    {
        CanDraw = can;
        if (!can && BrushCursor != null)
        {
            BrushCursor.Visible = false;
        }
    }

    /// <summary>
    /// 设置画笔大小
    /// </summary>
    public void SetBrushSize(int size)
    {
        if (BrushCursor != null)
        {
            BrushCursor.Size = size;
        }
    }

    /// <summary>
    /// 调整地图尺寸
    /// </summary>
    public void ResizeMap(int newWidth, int newHeight)
    {
        Ground?.Resize(newWidth, newHeight);
        EmitSignal(SignalName.MapUpdated);
    }

    /// <summary>
    /// 设置选中的地表、覆盖物和地高类型
    /// </summary>
    public void SetSelectedTypes(SurfaceType.Enums surface, OverlayType.Enums overlay, TerrainHeight.Enums height = TerrainHeight.Enums.PLAIN)
    {
        SelectedSurface = surface;
        SelectedOverlay = overlay;
        SelectedHeight = height;
    }

    /// <summary>
    /// 设置是否处于建筑编辑模式
    /// </summary>
    public void SetBuildingEditorActive(bool active, string type = "", int countryId = -1)
    {
        IsBuildingEditorActive = active;
        CurrentSelectedBuilding = type;
        CurrentSelectedCountryId = countryId;
        // 建筑编辑模式下不直接更新地块数据
        IsUpdatingGround = !active;
    }

    /// <summary>
    /// 清除区域渲染
    /// </summary>
    public void ClearAreaRender()
    {
        GridAreaRender?.Clear();
    }

    private void onCellsDrawn(Godot.Collections.Array<Vector2I> cells)
    {
        if (LayerManager != null && Ground != null && !IsBuildingEditorActive)
        {
            LayerManager.UpdateCells(cells, Ground);
        }
    }

    private void onDrawCompleted()
    {
        if (LayerManager != null && Ground != null)
        {
            LayerManager.UpdateMapData(Ground);
        }
    }

    /// <summary>
    /// 转发信号
    /// </summary>
    public void EmitCellsDrawn(Godot.Collections.Array<Vector2I> cells)
    {
        EmitSignal(SignalName.CellsDrawn, cells);
    }

    /// <summary>
    /// 转发信号
    /// </summary>
    public void EmitMapDrawCompleted()
    {
        EmitSignal(SignalName.DrawCompleted);
    }

    /// <summary>
    /// 设置光标位置
    /// </summary>
    public void SetCursorPosition(Vector3 pos)
    {
        if (BrushCursor != null) BrushCursor.GlobalPosition = pos;
    }

    /// <summary>
    /// 设置光标是否可见
    /// </summary>
    public void SetCursorVisible(bool visible)
    {
        if (BrushCursor != null) BrushCursor.Visible = visible;
    }

    /// <summary>
    /// 设置禁用图标是否可见
    /// </summary>
    public void SetForbiddenVisible(bool visible)
    {
        if (BrushCursor != null) BrushCursor.SetForbidden(visible);
    }

    /// <summary>
    /// 将地格坐标转换为世界坐标
    /// </summary>
    public Vector3 GridToWorld(Vector2I gridPos)
    {
        if (Ground == null) return Vector3.Zero;
        float targetY = YConfig.CursorY;
        Grid? grid = Ground.GetGrid(gridPos.X, gridPos.Y);
        if (grid != null)
        {
            float heightY = YConfig.PlainY;
            if (grid.Height == TerrainHeight.Enums.HILL) heightY = YConfig.HillY;
            else if (grid.Height == TerrainHeight.Enums.MOUNTAIN) heightY = YConfig.MountainY;
            targetY = heightY + 0.02f;
        }

        return Ground.GridToWorld(gridPos, targetY);
    }
}
