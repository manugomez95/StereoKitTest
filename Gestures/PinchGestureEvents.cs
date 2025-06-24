using StereoKit;

namespace StereoKitQuest3.Gestures;

// Event system for pinch gestures
public static class PinchGestureEvents
{
	public delegate void PinchEventHandler(bool isRightHand, Vec3 position);
	
	public static event PinchEventHandler OnPinchStart;
	public static event PinchEventHandler OnPinchEnd;
	public static event PinchEventHandler OnPinchHold;
	
	// Helper methods for invoking events from outside the class
	public static void InvokePinchStart(bool isRightHand, Vec3 position)
	{
		OnPinchStart?.Invoke(isRightHand, position);
	}
	
	public static void InvokePinchEnd(bool isRightHand, Vec3 position)
	{
		OnPinchEnd?.Invoke(isRightHand, position);
	}
	
	public static void InvokePinchHold(bool isRightHand, Vec3 position)
	{
		OnPinchHold?.Invoke(isRightHand, position);
	}
} 