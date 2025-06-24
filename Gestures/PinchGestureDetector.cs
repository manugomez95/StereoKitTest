using StereoKit;
using System;

namespace StereoKitQuest3.Gestures;

// Pinch gesture detector class
public class PinchGestureDetector
{
	private bool _rightHandWasPinching = false;
	private bool _leftHandWasPinching = false;
	private float _pinchThreshold = 0.7f;  // Higher threshold for better reliability on Quest 3
	private float _releaseThreshold = 0.3f; // Lower threshold for release to prevent flickering
	private DateTime _rightPinchStartTime;
	private DateTime _leftPinchStartTime;
	private Vec3 _rightPinchPosition;
	private Vec3 _leftPinchPosition;
	
	public void Update()
	{
		UpdateHandPinch(Handed.Right);
		UpdateHandPinch(Handed.Left);
	}
	
	private void UpdateHandPinch(Handed handedness)
	{
		Hand hand = Input.Hand(handedness);
		bool isRightHand = handedness == Handed.Right;
		
		if (!hand.IsTracked)
			return;
			
		bool wasWasPinching = isRightHand ? _rightHandWasPinching : _leftHandWasPinching;
		float pinchValue = hand.pinchActivation;
		
		// Calculate pinch position (midpoint between index tip and thumb tip)
		Vec3 pinchPosition = (hand[FingerId.Index, JointId.Tip].position + 
							 hand[FingerId.Thumb, JointId.Tip].position) * 0.5f;
		
		// Determine current pinch state using hysteresis for stability
		bool isPinching = wasWasPinching ? 
			pinchValue > _releaseThreshold : 
			pinchValue > _pinchThreshold;
		
		// Update state
		if (isRightHand)
		{
			_rightHandWasPinching = isPinching;
			_rightPinchPosition = pinchPosition;
		}
		else
		{
			_leftHandWasPinching = isPinching;
			_leftPinchPosition = pinchPosition;
		}
		
		// Fire events
		if (!wasWasPinching && isPinching)
		{
			// Pinch started
			if (isRightHand)
				_rightPinchStartTime = DateTime.Now;
			else
				_leftPinchStartTime = DateTime.Now;
				
			PinchGestureEvents.InvokePinchStart(isRightHand, pinchPosition);
			Log.Info($"{(isRightHand ? "Right" : "Left")} hand pinch started at {pinchPosition}");
		}
		else if (wasWasPinching && !isPinching)
		{
			// Pinch ended
			DateTime startTime = isRightHand ? _rightPinchStartTime : _leftPinchStartTime;
			double duration = (DateTime.Now - startTime).TotalSeconds;
			
			PinchGestureEvents.InvokePinchEnd(isRightHand, pinchPosition);
			Log.Info($"{(isRightHand ? "Right" : "Left")} hand pinch ended. Duration: {duration:F2}s");
		}
		else if (isPinching)
		{
			// Pinch holding
			PinchGestureEvents.InvokePinchHold(isRightHand, pinchPosition);
		}
	}
	
	public bool IsRightHandPinching => _rightHandWasPinching;
	public bool IsLeftHandPinching => _leftHandWasPinching;
	public Vec3 RightPinchPosition => _rightPinchPosition;
	public Vec3 LeftPinchPosition => _leftPinchPosition;
} 