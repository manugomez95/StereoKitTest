using StereoKit;
using StereoKitQuest3.Gestures;
using StereoKitQuest3.Workflows;
using System;

namespace StereoKitQuest3;

public class App
{
	private PinchGestureDetector _pinchDetector;
	private RecordingWorkflow _recordingWorkflow;
	private PassthroughFBExt _passthroughStepper;
	
	// UI state
	private Pose _windowPose = new Pose(0, 1.5f, -0.5f, Quat.LookDir(0, 0, 1));
	private Pose _cubePose = new Pose(0, 0, -0.5f);
	private Model _cube;
	private Matrix _floorTransform;
	private Material _floorMaterial;
	
	public void Initialize()
	{
		// Add passthrough stepper BEFORE initializing StereoKit
		Log.Info("MAN-27: Adding PassthroughFBExt stepper...");
		_passthroughStepper = SK.AddStepper(new PassthroughFBExt());
		Log.Info("MAN-27: PassthroughFBExt stepper added successfully");
		
		// Initialize StereoKit with Quest 3 optimized settings
		SKSettings settings = new SKSettings
		{
			appName = "StereoKitQuest3",
			assetsFolder = "Assets",
			mode = AppMode.XR
		};
		
		Log.Info("MAN-27: Initializing StereoKit...");
		if (!SK.Initialize(settings))
			throw new InvalidOperationException("Failed to initialize StereoKit");
		Log.Info("MAN-27: StereoKit initialized successfully");

		// Log backend information
		Log.Info($"MAN-27: Backend XR Type: {Backend.XRType}");
		Log.Info($"MAN-27: Device Name: {Device.Name}");
		Log.Info($"MAN-27: Display Blend: {Device.DisplayBlend}");

		// Enable passthrough AFTER StereoKit is initialized
		Log.Info("MAN-27: Checking passthrough availability...");
		if (_passthroughStepper.Available)
		{
			Log.Info("MAN-27: Passthrough extension is available, enabling...");
			_passthroughStepper.EnabledPassthrough = true;
			Log.Info($"MAN-27: Passthrough enabled status: {_passthroughStepper.EnabledPassthrough}");
		}
		else
		{
			Log.Warn("MAN-27: Passthrough extension not available");
			Log.Info($"MAN-27: Backend XR Type: {Backend.XRType}");
			if (Backend.XRType == BackendXRType.OpenXR)
			{
				bool extEnabled = Backend.OpenXR.ExtEnabled("XR_FB_passthrough");
				Log.Info($"MAN-27: XR_FB_passthrough extension enabled: {extEnabled}");
			}
		}

		// Log hand tracking availability
		Log.Info("Hand tracking setup - ensure permissions are granted in device settings");
		Log.Info("MAN-11: Pinch gesture detection system initialized");

		// Initialize pinch gesture detection and recording workflow
		_pinchDetector = new PinchGestureDetector();
		_recordingWorkflow = new RecordingWorkflow();
		_recordingWorkflow.Initialize();

		// Create assets used by the app
		_cube = Model.FromMesh(
			Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
			Material.UI);

		_floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
		_floorMaterial = new Material("floor.hlsl");
		_floorMaterial.Transparency = Transparency.Blend;
	}
	
	public void Run()
	{
		SK.Run(Update);
	}
	
	private void Update()
	{
		// Update pinch gesture detection
		_pinchDetector.Update();
		
		if (Device.DisplayBlend == DisplayBlend.Opaque)
			Mesh.Cube.Draw(_floorMaterial, _floorTransform);

		UI.Handle("Cube", ref _cubePose, _cube.Bounds);
		_cube.Draw(_cubePose.ToMatrix());

		// Hand tracking status window
		UI.WindowBegin("Quest 3 Hand Tracking & Gestures", ref _windowPose, new Vec2(30, 0) * U.cm);
		
		RenderHandTrackingUI();
		RenderPinchGestureUI();
		RenderRecordingWorkflowUI();
		
		UI.WindowEnd();
	}
	
	private void RenderHandTrackingUI()
	{
		// Display hand tracking status
		Hand rightHand = Input.Hand(Handed.Right);
		Hand leftHand = Input.Hand(Handed.Left);
		
		UI.Label($"Right Hand: {(rightHand.IsTracked ? "Tracked" : "Not Tracked")}");
		UI.Label($"Left Hand: {(leftHand.IsTracked ? "Tracked" : "Not Tracked")}");
		
		// Display device info and passthrough status
		UI.Label($"Device: {Device.Name}");
		
		// Enhanced passthrough status display
		if (_passthroughStepper != null && _passthroughStepper.Available)
		{
			bool passthroughActive = _passthroughStepper.EnabledPassthrough;
			UI.Label($"Passthrough: {(passthroughActive ? "ENABLED" : "Disabled")}");
			UI.Label($"Display Mode: {Device.DisplayBlend}");
		}
		else
		{
			UI.Label($"Passthrough: Not Available");
			UI.Label($"Display Mode: {Device.DisplayBlend}");
		}
	}
	
	private void RenderPinchGestureUI()
	{
		UI.HSeparator();
		UI.Text("Pinch Gesture Detection (MAN-11):", TextAlign.CenterLeft);
		
		// Enhanced pinch gesture status
		Hand rightHand = Input.Hand(Handed.Right);
		Hand leftHand = Input.Hand(Handed.Left);
		
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
	}
	
	private void RenderRecordingWorkflowUI()
	{
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
	}
} 