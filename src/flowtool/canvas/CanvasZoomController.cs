using Godot;

/// <summary>
/// 画布缩放控制器，处理鼠标滚轮驱动的摄像机缩放。
/// </summary>
public sealed class CanvasZoomController
{
	// 最小缩放比例。
	private const float minZoom = 0.35f;
	// 最大缩放比例。
	private const float maxZoom = 2.8f;
	// 每次滚轮缩放步进。
	private const float zoomStep = 0.12f;

	/// <summary>
	/// 处理滚轮缩放输入。
	/// </summary>
	public bool TryHandle(InputEventMouseButton mouseButton, Camera2D camera)
	{
		if (mouseButton.Pressed == false)
		{
			return false;
		}

		float zoomDelta = mouseButton.ButtonIndex switch
		{
			MouseButton.WheelUp => -zoomStep,
			MouseButton.WheelDown => zoomStep,
			_ => 0f
		};
		if (Mathf.IsZeroApprox(zoomDelta))
		{
			return false;
		}

		float currentZoom = camera.Zoom.X;
		float targetZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);
		camera.Zoom = new Vector2(targetZoom, targetZoom);
		return true;
	}
}
