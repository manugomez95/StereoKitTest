using StereoKit;

namespace StereoKitQuest3;

// Event system for pinch gestures
public static class PinchGestureEvents
{
	public delegate void PinchEventHandler(bool isRightHand, Vec3 position);
	
	public static event PinchEventHandler OnPinchStart;
	public static event PinchEventHandler OnPinchEnd;
	public static event PinchEventHandler OnPinchHold;
}

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
				
			PinchGestureEvents.OnPinchStart?.Invoke(isRightHand, pinchPosition);
			Log.Info($"{(isRightHand ? "Right" : "Left")} hand pinch started at {pinchPosition}");
		}
		else if (wasWasPinching && !isPinching)
		{
			// Pinch ended
			DateTime startTime = isRightHand ? _rightPinchStartTime : _leftPinchStartTime;
			double duration = (DateTime.Now - startTime).TotalSeconds;
			
			PinchGestureEvents.OnPinchEnd?.Invoke(isRightHand, pinchPosition);
			Log.Info($"{(isRightHand ? "Right" : "Left")} hand pinch ended. Duration: {duration:F2}s");
		}
		else if (isPinching)
		{
			// Pinch holding
			PinchGestureEvents.OnPinchHold?.Invoke(isRightHand, pinchPosition);
		}
	}
	
	public bool IsRightHandPinching => _rightHandWasPinching;
	public bool IsLeftHandPinching => _leftHandWasPinching;
	public Vec3 RightPinchPosition => _rightPinchPosition;
	public Vec3 LeftPinchPosition => _leftPinchPosition;
}

// Recording workflow manager
public class RecordingWorkflow
{
	private bool _isRecording = false;
	private DateTime _recordingStartTime;
	private List<string> _recordingLog = new List<string>();
	
	public bool IsRecording => _isRecording;
	public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
	
	public void Initialize()
	{
		// Subscribe to pinch events
		PinchGestureEvents.OnPinchStart += OnPinchStart;
		PinchGestureEvents.OnPinchEnd += OnPinchEnd;
		PinchGestureEvents.OnPinchHold += OnPinchHold;
	}
	
	private void OnPinchStart(bool isRightHand, Vec3 position)
	{
		if (isRightHand && !_isRecording)
		{
			StartRecording(position);
		}
	}
	
	private void OnPinchEnd(bool isRightHand, Vec3 position)
	{
		if (isRightHand && _isRecording)
		{
			StopRecording(position);
		}
	}
	
	private void OnPinchHold(bool isRightHand, Vec3 position)
	{
		if (isRightHand && _isRecording)
		{
			// Log holding position for potential gesture tracking
			_recordingLog.Add($"Hold at {position} - Duration: {RecordingDuration.TotalSeconds:F2}s");
		}
	}
	
	private void StartRecording(Vec3 position)
	{
		_isRecording = true;
		_recordingStartTime = DateTime.Now;
		_recordingLog.Clear();
		_recordingLog.Add($"Recording started at {position}");
		Log.Info("Recording workflow started via right-hand pinch gesture");
	}
	
	private void StopRecording(Vec3 position)
	{
		_isRecording = false;
		_recordingLog.Add($"Recording ended at {position} - Total duration: {RecordingDuration.TotalSeconds:F2}s");
		
		Log.Info($"Recording workflow stopped. Duration: {RecordingDuration.TotalSeconds:F2}s");
		
		// Here you could save the recording data, process gestures, etc.
		ProcessRecording();
	}
	
	private void ProcessRecording()
	{
		Log.Info($"Processing recording with {_recordingLog.Count} events");
		// Add recording processing logic here
	}
	
	public void GetRecordingStatus(out bool isRecording, out double duration, out int eventCount)
	{
		isRecording = _isRecording;
		duration = RecordingDuration.TotalSeconds;
		eventCount = _recordingLog.Count;
	}
}

class Program
{
	private static PinchGestureDetector _pinchDetector;
	private static RecordingWorkflow _recordingWorkflow;
	
	static void Main(string[] args)
	{
		// Initialize StereoKit with Quest 3 optimized settings
		SKSettings settings = new SKSettings
		{
			appName = "StereoKitQuest3",
			assetsFolder = "Assets",
			mode = AppMode.XR
		};
		if (!SK.Initialize(settings))
			return;

		// Log hand tracking availability
		Log.Info("Hand tracking setup - ensure permissions are granted in device settings");
		Log.Info("MAN-11: Pinch gesture detection system initialized");

		// Initialize pinch gesture detection and recording workflow
		_pinchDetector = new PinchGestureDetector();
		_recordingWorkflow = new RecordingWorkflow();
		_recordingWorkflow.Initialize();

		// Create assets used by the app
		Pose  cubePose = new Pose(0, 0, -0.5f);
		Model cube     = Model.FromMesh(
			Mesh.GenerateRoundedCube(Vec3.One*0.1f, 0.02f),
			Material.UI);

		Matrix   floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
		Material floorMaterial  = new Material("floor.hlsl");
		floorMaterial.Transparency = Transparency.Blend;

		// UI for status display
		Pose windowPose = new Pose(0, 1.5f, -0.5f, Quat.LookDir(0, 0, 1));

		// Core application loop
		SK.Run(() => {
			// Update pinch gesture detection
			_pinchDetector.Update();
			
			if (Device.DisplayBlend == DisplayBlend.Opaque)
				Mesh.Cube.Draw(floorMaterial, floorTransform);

			UI.Handle("Cube", ref cubePose, cube.Bounds);
			cube.Draw(cubePose.ToMatrix());

			// Hand tracking status window
			UI.WindowBegin("Quest 3 Hand Tracking & Gestures", ref windowPose, new Vec2(30, 0) * U.cm);
			
			// Display hand tracking status
			Hand rightHand = Input.Hand(Handed.Right);
			Hand leftHand = Input.Hand(Handed.Left);
			
			UI.Label($"Right Hand: {(rightHand.IsTracked ? "Tracked" : "Not Tracked")}");
			UI.Label($"Left Hand: {(leftHand.IsTracked ? "Tracked" : "Not Tracked")}");
			
			// Display device info
			UI.Label($"Device: {Device.Name}");
			UI.Label($"Display: {Device.DisplayBlend}");
			
			UI.HSeparator();
			UI.Text("Pinch Gesture Detection (MAN-11):", TextAlign.CenterLeft);
			
			// Enhanced pinch gesture status
			if (rightHand.IsTracked)
			{
				bool isPinching = _pinchDetector.IsRightHandPinching;
				UI.Label($"Right Pinch: {(isPinching ? "ACTIVE" : "Inactive")}");
				if (isPinching)
				{
					Vec3 pos = _pinchDetector.RightPinchPosition;
					UI.Label($"  Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
				}
				UI.Label($"  Raw Value: {rightHand.pinchActivation:F3}");
			}
			
			if (leftHand.IsTracked)
			{
				bool isPinching = _pinchDetector.IsLeftHandPinching;
				UI.Label($"Left Pinch: {(isPinching ? "ACTIVE" : "Inactive")}");
				if (isPinching)
				{
					Vec3 pos = _pinchDetector.LeftPinchPosition;
					UI.Label($"  Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
				}
				UI.Label($"  Raw Value: {leftHand.pinchActivation:F3}");
			}
			
			UI.HSeparator();
			UI.Text("Recording Workflow:", TextAlign.CenterLeft);
			
			// Recording workflow status
			_recordingWorkflow.GetRecordingStatus(out bool isRecording, out double duration, out int eventCount);
			UI.Label($"Recording: {(isRecording ? "ACTIVE" : "Inactive")}");
			if (isRecording)
			{
				UI.Label($"Duration: {duration:F1}s");
				UI.Label($"Events: {eventCount}");
			}
			UI.Text("Tip: Right-hand pinch to start/stop recording", TextAlign.CenterLeft);
			
			UI.WindowEnd();
		});
	}
}