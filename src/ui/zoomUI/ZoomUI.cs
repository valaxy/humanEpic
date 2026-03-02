using Godot;

/// <summary>
/// 镜头缩放控制 UI，提供缩放滑块与阈值刻度显示。
/// </summary>
[GlobalClass]
public partial class ZoomUI : CanvasLayer
{
	[Signal]
	public delegate void ZoomValueChangedEventHandler(float percent);

	/// <summary>交互停止后自动淡出的延迟秒数</summary>
	[Export]
	public float FadeDelay { get; set; } = 2.0f;

	/// <summary>UI 淡入淡出动画时长</summary>
	[Export]
	public float FadeDuration { get; set; } = 0.5f;

	// 缩放滑块控件。
	private HSlider slider = null!;
	// UI 根节点。
	private Control controlRoot = null!;
	// 阈值刻度线。
	private ColorRect marker = null!;
	// 自动隐藏补间。
	private Tween fadeTween = null!;
	// 绑定的摄像机。
	private GameCamera camera = null!;
	// 绑定的层级管理器。
	private LayerManagerNode layerManager = null!;
	// 战略视图模式状态。
	private bool isInStrategyViewMode;

	public override void _Ready()
	{
		slider = GetNode<HSlider>("%HSlider");
		controlRoot = GetNode<Control>("Control");

		slider.ValueChanged += onSliderValueChanged;
		setupMarker();
		controlRoot.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	}

	/// <summary>
	/// 初始化关联的摄像机与层级管理器。
	/// </summary>
	public void Setup(GameCamera cameraNode, LayerManagerNode managerNode)
	{
		camera = cameraNode;
		layerManager = managerNode;

		camera.ZoomChanged += onCameraZoomChanged;
		ZoomValueChanged += camera.SetZoomPercentage;

		syncWithCamera();
		updateMarkerPos();
	}

	// 从摄像机同步当前缩放状态。
	private void syncWithCamera()
	{
		if (camera == null)
		{
			return;
		}

		onCameraZoomChanged(camera.GlobalPosition.Y, camera.MinZoom, camera.MaxZoom);
		updateSlider(camera.GetZoomPercentage());
	}

	// 响应摄像机缩放变化。
	private void onCameraZoomChanged(float value, float minVal, float maxVal)
	{
		float percent = (value - minVal) / (maxVal - minVal);
		updateSlider(percent);

		if (layerManager != null && layerManager.IsNodeReady())
		{
			layerManager.HandleZoom(value);
		}

		if (layerManager == null)
		{
			return;
		}

		float strategyThreshold = layerManager.StrategyViewThreshold;
		bool strategyThresholdMet = value > strategyThreshold;
		if (strategyThresholdMet != isInStrategyViewMode)
		{
			isInStrategyViewMode = strategyThresholdMet;
		}
	}

	// 初始化滑块阈值刻度线。
	private void setupMarker()
	{
		marker = new ColorRect();
		marker.Color = new Color(1.0f, 1.0f, 0.0f, 0.5f);
		marker.MouseFilter = Control.MouseFilterEnum.Ignore;
		slider.AddChild(marker);

		slider.Resized += updateMarkerPos;
	}

	// 更新阈值刻度位置。
	private void updateMarkerPos()
	{
		if (camera == null || layerManager == null || marker == null)
		{
			return;
		}

		float totalHeight = slider.Size.Y;
		float totalWidth = slider.Size.X;

		float threshold = layerManager.StrategyViewThreshold;
		float minZoom = camera.MinZoom;
		float maxZoom = camera.MaxZoom;

		float progressSwitch = (threshold - minZoom) / (maxZoom - minZoom);
		progressSwitch = Mathf.Clamp(progressSwitch, 0.0f, 1.0f);

		marker.Size = new Vector2(2.0f, totalHeight);
		marker.Position = new Vector2(progressSwitch * totalWidth, 0.0f);
	}

	// 响应滑块值变化。
	private void onSliderValueChanged(double value)
	{
		showUi();
		EmitSignal(SignalName.ZoomValueChanged, (float)value);
	}

	// 外部更新滑块值。
	private void updateSlider(float percent)
	{
		if (slider == null)
		{
			return;
		}

		slider.ValueChanged -= onSliderValueChanged;

		if (!Mathf.IsEqualApprox((float)slider.Value, percent))
		{
			showUi();
			slider.Value = percent;
		}

		slider.ValueChanged += onSliderValueChanged;
	}

	// 显示 UI 并重置自动隐藏动画。
	private void showUi()
	{
		if (fadeTween != null && fadeTween.IsRunning())
		{
			fadeTween.Kill();
		}

		controlRoot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);

		fadeTween = CreateTween();
		fadeTween.SetTrans(Tween.TransitionType.Sine);
		fadeTween.SetEase(Tween.EaseType.InOut);
		fadeTween.TweenInterval(FadeDelay);
		fadeTween.TweenProperty(controlRoot, "modulate:a", 0.0f, FadeDuration);
	}
}
