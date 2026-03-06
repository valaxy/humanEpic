using Godot;


/// <summary>
/// 游戏摄像机节点，处理移动、缩放和坐标投影逻辑
/// </summary>
[GlobalClass]
public partial class GameCamera : Camera3D
{
    private const float RayParallelEpsilon = 0.0001f;
    private const float FocusShiftScale = 2.0f;

    public float MoveSpeed = 80.0f; // 摄像机平移速度
    public float ZoomSpeed = 2.0f; // 摄像机缩放步长 (滚轮)
    public float KeyboardZoomSpeed = 20.0f; // 键盘缩放速度 (每秒)
    public float ZoomLerpSpeed = 10.0f; // 缩放平滑过渡速度
    public float MoveLerpSpeed = 8.0f; // 视点平滑移动速度

    /// <summary>最小缩放高度</summary>
    public float MinZoom = 10.0f;
    /// <summary>最大缩放高度</summary>
    public float MaxZoom = 200.0f;
    /// <summary>初始缩放高度</summary>
    public float InitialZoom = 10.0f;

    public float MinAngleDeg = 30.0f; // 低空时的倾斜角度 (度)
    public float MaxAngleDeg = 80.0f; // 高空时的俯视角度 (度)

    [Signal] public delegate void ZoomChangedEventHandler(float value, float minVal, float maxVal);

    private float targetZoom;
    private float currentZoom;

    private Vector3 targetFocusPoint;
    private Vector3 currentFocusPoint;

    public override void _Ready()
    {
        targetZoom = InitialZoom;
        currentZoom = targetZoom;

        initializeCameraState();
    }

    /// <summary>
    /// 根据当前摄像机位置和角度初始化视点
    /// </summary>
    private void initializeCameraState()
    {
        Vector2 center = GetViewport().GetVisibleRect().Size / 2.0f;
        Vector3? groundPos = ProjectToPlane(center);
        targetFocusPoint = groundPos ?? getDefaultFocusPoint();
        currentFocusPoint = targetFocusPoint;
    }

    /// <summary>
    /// 获取默认视点。
    /// </summary>
    private Vector3 getDefaultFocusPoint()
    {
        return new Vector3(GlobalPosition.X, 0, GlobalPosition.Z - 10);
    }

    public override void _Process(double delta)
    {
        float fDelta = (float)delta;
        handleMovement(fDelta);
        updateCameraTransform(fDelta);
    }

    /// <summary>
    /// 处理摄像机的键盘移动和缩放输入
    /// </summary>
    private void handleMovement(float delta)
    {
        Vector3 inputDir = getMoveInputDirection();
        if (inputDir != Vector3.Zero)
        {
            targetFocusPoint += inputDir * MoveSpeed * delta;
        }

        // Q 和 E 控制视野高低
        if (Input.IsKeyPressed(Key.Q)) ZoomBy(-KeyboardZoomSpeed * delta);
        if (Input.IsKeyPressed(Key.E)) ZoomBy(KeyboardZoomSpeed * delta);
    }

    // 读取并归一化键盘移动方向。
    private static Vector3 getMoveInputDirection()
    {
        Vector3 inputDir = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W)) inputDir.Z -= 1.0f;
        if (Input.IsKeyPressed(Key.S)) inputDir.Z += 1.0f;
        if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1.0f;
        if (Input.IsKeyPressed(Key.D)) inputDir.X += 1.0f;
        return inputDir == Vector3.Zero ? Vector3.Zero : inputDir.Normalized();
    }

    /// <summary>
    /// 每一帧更新摄像机位置和旋转
    /// </summary>
    private void updateCameraTransform(float delta)
    {
        float prevZoom = currentZoom;

        // 平滑插值
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, ZoomLerpSpeed * delta);
        currentFocusPoint = currentFocusPoint.Lerp(targetFocusPoint, MoveLerpSpeed * delta);

        float t = getZoomRatio(currentZoom);

        // 根据高度计算倾斜角度 (动态角度)
        float angleRad = Mathf.DegToRad(Mathf.Lerp(MinAngleDeg, MaxAngleDeg, t));

        // 根据高度和角度计算 Z 轴偏移量
        float zOffset = currentZoom / Mathf.Tan(angleRad);

        // 设置摄像机空间位置并注视当前视点
        GlobalPosition = currentFocusPoint + new Vector3(0, currentZoom, zOffset);
        LookAt(currentFocusPoint, Vector3.Up);

        // 发送信号更新 UI (仅在高度变动足够大时)
        if (!Mathf.IsEqualApprox(currentZoom, prevZoom))
        {
            EmitSignal(SignalName.ZoomChanged, currentZoom, MinZoom, MaxZoom);
        }
    }

    // 将当前高度映射为 0-1 缩放区间。
    private float getZoomRatio(float zoom)
    {
        if (Mathf.IsZeroApprox(MaxZoom - MinZoom))
        {
            return 0.0f;
        }

        float rawRatio = (zoom - MinZoom) / (MaxZoom - MinZoom);
        return Mathf.Clamp(rawRatio, 0.0f, 1.0f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomBy(-ZoomSpeed, mouseButton.Position);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomBy(ZoomSpeed, mouseButton.Position);
            }
        }
    }

    /// <summary>
    /// 按指定增量更新目标缩放值，并实现向目标点平滑偏移
    /// </summary>
    public void ZoomBy(float amount, Vector2 mousePos = default)
    {
        float prevTargetZoom = targetZoom;
        targetZoom = Mathf.Clamp(targetZoom + amount, MinZoom, MaxZoom);

        // 缩放至鼠标指向点逻辑
        if (Mathf.IsEqualApprox(prevTargetZoom, targetZoom))
        {
            return;
        }

        Vector2 targetScreenPos = mousePos == default
            ? GetViewport().GetVisibleRect().Size / 2.0f
            : mousePos;

        Vector3? groundPos = ProjectToPlane(targetScreenPos);
        if (!groundPos.HasValue)
        {
            return;
        }

        // 动态调整视点，产生“缩放至目标”的效果
        float zoomRange = MaxZoom - MinZoom;
        if (Mathf.IsZeroApprox(zoomRange))
        {
            return;
        }

        float shiftFactor = (targetZoom - prevTargetZoom) / zoomRange;
        targetFocusPoint -= (groundPos.Value - targetFocusPoint) * shiftFactor * FocusShiftScale;
    }

    /// <summary>
    /// 获取指定屏幕坐标对应的平面坐标
    /// </summary>
    /// <param name="mousePos">屏幕鼠标位置</param>
    /// <param name="planeY">目标平面高度</param>
    /// <returns>交点坐标，如果不相交返回 null</returns>
    public Vector3? ProjectToPlane(Vector2 mousePos, float planeY = 0.0f)
    {
        Vector3 rayOrigin = ProjectRayOrigin(mousePos);
        Vector3 rayDir = ProjectRayNormal(mousePos);
        
        if (Mathf.Abs(rayDir.Y) < RayParallelEpsilon)
        {
            return null;
        }
            
        float t = (planeY - rayOrigin.Y) / rayDir.Y;
        if (t < 0)
        {
            return null;
        }
            
        return rayOrigin + rayDir * t;
    }

    /// <summary>
    /// 将屏幕坐标解析为有效地格坐标。
    /// </summary>
    /// <param name="screenPos">屏幕坐标。</param>
    /// <param name="ground">地面模型。</param>
    /// <param name="cellPos">解析后的地格坐标。</param>
    /// <param name="planeY">投影平面高度。</param>
    /// <returns>是否成功解析到地图边界内地格。</returns>
    public bool TryResolveGroundCell(Vector2 screenPos, Ground ground, out Vector2I cellPos, float planeY = 0.0f)
    {
        cellPos = Vector2I.Zero;

        Vector3? worldPoint = ProjectToPlane(screenPos, planeY);
        if (!worldPoint.HasValue)
        {
            return false;
        }

        Vector2 cellFloat = ground.WorldToGrid(worldPoint.Value);
        Vector2I resolved = new Vector2I(Mathf.FloorToInt(cellFloat.X), Mathf.FloorToInt(cellFloat.Y));
        if (!ground.IsInsideGround(resolved))
        {
            return false;
        }

        cellPos = resolved;
        return true;
    }

    /// <summary>
    /// 获取当前鼠标与地面平面的交点。
    /// </summary>
    /// <param name="planeY">投影平面高度。</param>
    /// <returns>交点坐标，若无交点则返回 null。</returns>
    public Vector3? GetRayIntersection(float planeY = 0.0f)
    {
        return ProjectToPlane(GetViewport().GetMousePosition(), planeY);
    }

    /// <summary>
    /// 设置缩放百分比 (0.0 到 1.0)
    /// </summary>
    public void SetZoomPercentage(float percent)
    {
        targetZoom = Mathf.Lerp(MinZoom, MaxZoom, percent);
    }

    /// <summary>
    /// 获取当前缩放百分比
    /// </summary>
    public float GetZoomPercentage()
    {
        return getZoomRatio(currentZoom);
    }
}
