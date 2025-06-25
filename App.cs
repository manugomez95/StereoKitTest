using StereoKit;
using StereoKit.Framework;
using StereoKitQuest3.Gestures;
using System;

namespace StereoKitQuest3;

public class App
{
	private PinchGestureDetector _pinchDetector;
	private PassthroughFBExt _passthroughStepper;
	
	// UI state
	private Pose _windowPose = new Pose(0, 1.5f, -0.5f, Quat.LookDir(0, 0, 1));
	private Pose _cubePose = new Pose(0, 0, -0.5f);
	private Model _cube;
	private Matrix _floorTransform;
	private Material _floorMaterial;
	
	public void Initialize()
	{
		// CRITICAL: Add passthrough stepper BEFORE initializing StereoKit
		Log.Info("MAN-27: Adding PassthroughFBExt stepper BEFORE StereoKit initialization...");
		_passthroughStepper = SK.AddStepper<PassthroughFBExt>();
		Log.Info("MAN-27: PassthroughFBExt stepper added successfully");
		
		// Initialize StereoKit with standard settings
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

		// Log backend and display information
		Log.Info($"MAN-27: Backend XR Type: {Backend.XRType}");
		Log.Info($"MAN-27: Device Name: {Device.Name}");
		Log.Info($"MAN-27: Display Blend: {Device.DisplayBlend}");
		Log.Info($"MAN-27: Passthrough Available: {_passthroughStepper.Available}");
		Log.Info($"MAN-27: Passthrough Enabled: {_passthroughStepper.EnabledPassthrough}");

		// Initialize game objects
		_cube = Model.FromMesh(
			Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
			Default.MaterialUI);

		_floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
		_floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
		_floorMaterial.Transparency = Transparency.Blend;

		// Initialize gesture detection
		_pinchDetector = new PinchGestureDetector();
		
		Log.Info("App initialized successfully");
	}

	public void Update()
	{
		// Update gesture detection
		_pinchDetector.Update();
		
		// Draw floor only in opaque mode (use Device.DisplayBlend instead of obsolete displayType)
		if (Device.DisplayBlend == DisplayBlend.Opaque)
			Default.MeshCube.Draw(_floorMaterial, _floorTransform);

		// Draw cube
		UI.Handle("Cube", ref _cubePose, _cube.Bounds);
		_cube.Draw(_cubePose.ToMatrix());
		
		// Draw UI window with enhanced debugging info
		UI.WindowBegin("StereoKit Quest 3", ref _windowPose);
		
		// Display information
		UI.Label($"Display: {Device.DisplayBlend}");
		
		// Passthrough information
		if (_passthroughStepper != null)
		{
			UI.Label($"Passthrough Available: {_passthroughStepper.Available}");
			UI.Label($"Passthrough Status: {(_passthroughStepper.EnabledPassthrough ? "ENABLED" : "DISABLED")}");
			
			if (_passthroughStepper.Available)
			{
				if (UI.Button("Toggle Passthrough"))
					_passthroughStepper.EnabledPassthrough = !_passthroughStepper.EnabledPassthrough;
			}
			else
			{
				UI.Label("Passthrough not available on this device");
			}
		}
		
		// Hand tracking info
		UI.Label($"Left Hand: {Input.Hand(Handed.Left).IsTracked}");
		UI.Label($"Right Hand: {Input.Hand(Handed.Right).IsTracked}");
		
		UI.WindowEnd();
	}

	public void Shutdown()
	{
		Log.Info("App shutdown complete");
	}
} 