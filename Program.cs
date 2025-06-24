using StereoKit;

namespace StereoKitQuest3;

class Program
{
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
			if (Device.DisplayBlend == DisplayBlend.Opaque)
				Mesh.Cube.Draw(floorMaterial, floorTransform);

			UI.Handle("Cube", ref cubePose, cube.Bounds);
			cube.Draw(cubePose.ToMatrix());

			// Hand tracking status window
			UI.WindowBegin("Quest 3 Status", ref windowPose, new Vec2(25, 0) * U.cm);
			
			// Display hand tracking status
			Hand rightHand = Input.Hand(Handed.Right);
			Hand leftHand = Input.Hand(Handed.Left);
			
			UI.Label($"Right Hand: {(rightHand.IsTracked ? "Tracked" : "Not Tracked")}");
			UI.Label($"Left Hand: {(leftHand.IsTracked ? "Tracked" : "Not Tracked")}");
			
			// Display device info
			UI.Label($"Device: {Device.Name}");
			UI.Label($"Display: {Device.DisplayBlend}");
			
			// Check for pinch gestures (preparation for future features)
			if (rightHand.IsTracked)
			{
				bool isPinching = rightHand.pinchActivation > 0.5f;
				UI.Label($"Right Pinch: {(isPinching ? "Active" : "Inactive")}");
			}
			
			UI.WindowEnd();
		});
	}
}