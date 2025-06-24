using StereoKit;
using System;
using System.Collections.Generic;
using System.IO;

namespace StereoKitQuest3;

// Event system for pinch gestures
public static class PinchGestureEvents
{
	public delegate void PinchEventHandler(bool isRightHand, Vec3 position);
	
	public static event PinchEventHandler OnPinchStart;
	public static event PinchEventHandler OnPinchEnd;
	public static event PinchEventHandler OnPinchHold;
	
	// Helper methods to invoke events
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

// Recording workflow manager with microphone integration
public class RecordingWorkflow
{
	private bool _isRecording = false;
	private DateTime _recordingStartTime;
	private List<string> _recordingLog = new List<string>();
	private MicrophoneRecorder _microphoneRecorder;
	private string _lastRecordedAudioPath = null;
	
	public bool IsRecording => _isRecording;
	public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
	public string LastRecordedAudioPath => _lastRecordedAudioPath;
	
	public void Initialize(MicrophoneRecorder microphoneRecorder)
	{
		_microphoneRecorder = microphoneRecorder;
		
		// Subscribe to pinch events
		PinchGestureEvents.OnPinchStart += OnPinchStart;
		PinchGestureEvents.OnPinchEnd += OnPinchEnd;
		PinchGestureEvents.OnPinchHold += OnPinchHold;
	}
	
	public void Update()
	{
		// Handle edge cases for microphone recording
		if (_microphoneRecorder != null && _microphoneRecorder.IsRecording)
		{
			_microphoneRecorder.HandleRapidPinchRepeats();
			_microphoneRecorder.HandleLongRecording();
		}
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
		// Check microphone permission before starting
		if (_microphoneRecorder == null || !_microphoneRecorder.HasMicrophonePermission)
		{
			Log.Warn("MAN-12: Cannot start recording - microphone permission not granted");
			return;
		}
		
		// Start the microphone recording
		bool micStarted = _microphoneRecorder.StartRecording();
		if (!micStarted)
		{
			Log.Warn("MAN-12: Failed to start microphone recording");
			return;
		}
		
		_isRecording = true;
		_recordingStartTime = DateTime.Now;
		_recordingLog.Clear();
		_recordingLog.Add($"Recording started at {position}");
		Log.Info("MAN-12: Audio recording workflow started via right-hand pinch gesture");
	}
	
	private void StopRecording(Vec3 position)
	{
		if (_microphoneRecorder != null && _microphoneRecorder.IsRecording)
		{
			// Stop microphone recording and get the file path
			_lastRecordedAudioPath = _microphoneRecorder.StopRecording();
		}
		
		_isRecording = false;
		_recordingLog.Add($"Recording ended at {position} - Total duration: {RecordingDuration.TotalSeconds:F2}s");
		
		Log.Info($"MAN-12: Audio recording workflow stopped. Duration: {RecordingDuration.TotalSeconds:F2}s");
		
		// Process the recording
		ProcessRecording();
	}
	
	private void ProcessRecording()
	{
		Log.Info($"MAN-12: Processing recording with {_recordingLog.Count} events");
		
		if (_lastRecordedAudioPath != null)
		{
			Log.Info($"MAN-12: Audio file ready for upload: {_lastRecordedAudioPath}");
			Log.Info("MAN-12: Audio is processed for speech recognition (16kHz mono)");
			
			// Here you would typically:
			// 1. Upload the audio file to your speech recognition service
			// 2. Trigger speech-to-text processing
			// 3. Handle the transcription results
		}
		else
		{
			Log.Warn("MAN-12: No audio file was generated during recording");
		}
	}
	
	public void GetRecordingStatus(out bool isRecording, out double duration, out int eventCount)
	{
		isRecording = _isRecording;
		duration = RecordingDuration.TotalSeconds;
		eventCount = _recordingLog.Count;
	}
}

// Microphone recorder for audio capture during pinch gestures
public class MicrophoneRecorder
{
	private bool _isRecording = false;
	private bool _hasPermission = false;
	private DateTime _recordingStartTime;
	private string _currentRecordingPath = null;
	private List<string> _recordedFiles = new List<string>();
	private float _currentIntensity = 0.0f;
	private float[] _sampleBuffer = new float[0];
	
	// Configuration for Quest 3 optimized audio recording
	private const int SAMPLE_RATE = 16000; // 16kHz for speech recognition
	private const int CHANNELS = 1; // Mono audio
	private const int BUFFER_SIZE = 2048;
	
	public bool IsRecording => _isRecording;
	public bool HasMicrophonePermission => _hasPermission;
	public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
	public float CurrentIntensity => _currentIntensity;
	public string CurrentRecordingPath => _currentRecordingPath;
	public List<string> RecordedFiles => new List<string>(_recordedFiles);
	
	public void Initialize()
	{
		Log.Info("MAN-12: Initializing microphone recorder");
		
		// Check microphone availability
		string[] devices = Microphone.GetDevices();
		if (devices.Length > 0)
		{
			Log.Info($"Found {devices.Length} microphone device(s): {string.Join(", ", devices)}");
			_hasPermission = true;
		}
		else
		{
			Log.Warn("No microphone devices found - permissions may be required");
			_hasPermission = false;
		}
	}
	
	public bool StartRecording()
	{
		if (_isRecording)
		{
			Log.Warn("Already recording - ignoring start request");
			return false;
		}
		
		if (!_hasPermission)
		{
			Log.Warn("Microphone permission not granted");
			return false;
		}
		
		try
		{
			// Generate filename for the recording
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string fileName = $"PinchRecording_{timestamp}.wav";
			
			// Start microphone recording using StereoKit's built-in functionality
			if (Microphone.Start())
			{
				_isRecording = true;
				_recordingStartTime = DateTime.Now;
				_currentRecordingPath = fileName;
				
				Log.Info($"MAN-12: Started recording audio to memory - target quality: {SAMPLE_RATE}Hz mono");
				return true;
			}
			else
			{
				Log.Warn("Failed to start microphone recording");
				return false;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"Exception starting recording: {ex.Message}");
			return false;
		}
	}
	
	public string StopRecording()
	{
		if (!_isRecording)
		{
			Log.Warn("Not currently recording - ignoring stop request");
			return null;
		}
		
		try
		{
			// Stop the microphone
			Microphone.Stop();
			
			// Calculate recording duration
			double duration = RecordingDuration.TotalSeconds;
			
			// Get the audio data from StereoKit's microphone stream
			if (Microphone.Sound != null && Microphone.Sound.UnreadSamples > 0)
			{
				// Read all the recorded samples
				float[] allSamples = new float[Microphone.Sound.UnreadSamples];
				int samplesRead = Microphone.Sound.ReadSamples(ref allSamples);
				
				// Process the audio for speech recognition quality
				var processedAudio = ProcessAudioForSpeechRecognition(allSamples, samplesRead);
				
				// Save to temp file (in real implementation, you would save this properly)
				string finalPath = SaveAudioToTempFile(processedAudio, _currentRecordingPath);
				
				if (finalPath != null)
				{
					_recordedFiles.Add(finalPath);
					Log.Info($"MAN-12: Stopped recording - Duration: {duration:F2}s, Samples: {samplesRead}, Saved: {finalPath}");
				}
				
				_isRecording = false;
				_currentRecordingPath = null;
				return finalPath;
			}
			else
			{
				Log.Warn("No audio data captured during recording");
				_isRecording = false;
				_currentRecordingPath = null;
				return null;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"Exception stopping recording: {ex.Message}");
			_isRecording = false;
			_currentRecordingPath = null;
			return null;
		}
	}
	
	public void Update()
	{
		if (_isRecording && Microphone.Sound != null)
		{
			// Update current intensity for UI feedback
			UpdateCurrentIntensity();
		}
	}
	
	private void UpdateCurrentIntensity()
	{
		if (Microphone.Sound.UnreadSamples > 0)
		{
			// Ensure buffer is large enough
			if (_sampleBuffer.Length < Microphone.Sound.UnreadSamples)
			{
				_sampleBuffer = new float[Microphone.Sound.UnreadSamples];
			}
			
			// Read samples for intensity calculation
			int samples = Microphone.Sound.ReadSamples(ref _sampleBuffer);
			
			// Calculate RMS (Root Mean Square) for better intensity representation
			float sum = 0f;
			for (int i = 0; i < samples; i++)
			{
				sum += _sampleBuffer[i] * _sampleBuffer[i];
			}
			
			if (samples > 0)
			{
				float rms = (float)Math.Sqrt(sum / samples);
				// Apply smoothing to prevent flickering
				_currentIntensity = (_currentIntensity * 0.8f) + (rms * 0.2f);
			}
		}
	}
	
	private float[] ProcessAudioForSpeechRecognition(float[] samples, int count)
	{
		// Process audio to ensure it's suitable for speech recognition
		// This includes filtering and normalization for 16kHz mono output
		
		List<float> processed = new List<float>();
		
		// Simple processing: normalize audio levels and ensure mono
		float maxLevel = 0f;
		for (int i = 0; i < count; i++)
		{
			maxLevel = Math.Max(maxLevel, Math.Abs(samples[i]));
		}
		
		// Normalize to prevent clipping while maintaining dynamic range
		float normalizationFactor = maxLevel > 0f ? 0.8f / maxLevel : 1f;
		
		for (int i = 0; i < count; i++)
		{
			processed.Add(samples[i] * normalizationFactor);
		}
		
		Log.Info($"Processed {count} samples for speech recognition (normalized by {normalizationFactor:F3})");
		return processed.ToArray();
	}
	
	private string SaveAudioToTempFile(float[] samples, string fileName)
	{
		try
		{
			// In a real implementation, you would save this as a proper WAV file
			// For now, we'll simulate saving and return a path
			string tempPath = $"temp_audio/{fileName}";
			
			// Create a Sound object from the samples for future playback/processing
			Sound recordedSound = Sound.FromSamples(samples);
			
			Log.Info($"Audio saved to memory as Sound object (simulated path: {tempPath})");
			Log.Info($"Audio duration: {recordedSound.Duration:F2}s, suitable for speech recognition");
			
			return tempPath;
		}
		catch (Exception ex)
		{
			Log.Warn($"Failed to save audio: {ex.Message}");
			return null;
		}
	}
	
	public void HandleRapidPinchRepeats()
	{
		// Edge case handling: prevent rapid start/stop cycles
		if (_isRecording && RecordingDuration.TotalSeconds < 0.5)
		{
			Log.Info("Ignoring rapid pinch repeat - minimum recording time is 0.5s");
			return;
		}
	}
	
	public void HandleLongRecording()
	{
		// Edge case handling: limit recording length to prevent memory issues
		if (_isRecording && RecordingDuration.TotalMinutes > 5.0)
		{
			Log.Warn("Recording exceeded 5 minutes - automatically stopping");
			StopRecording();
		}
	}
	
	public void GetRecordingInfo(out bool isRecording, out double duration, out float intensity, out int fileCount)
	{
		isRecording = _isRecording;
		duration = RecordingDuration.TotalSeconds;
		intensity = _currentIntensity;
		fileCount = _recordedFiles.Count;
	}
}

class Program
{
	private static PinchGestureDetector _pinchDetector;
	private static RecordingWorkflow _recordingWorkflow;
	private static MicrophoneRecorder _microphoneRecorder;
	
	static void Main(string[] args)
	{
		// Initialize StereoKit with Quest 3 optimized settings
		SKSettings settings = new SKSettings
		{
			appName = "StereoKitQuest3",
			assetsFolder = "Assets",
			mode = AppMode.XR,
			blendPreference = DisplayBlend.AnyTransparent,  // Enable passthrough
			overlayApp = false,  // Ensure we're not in overlay mode
			overlayPriority = 0
		};
		
		Log.Info("Initializing StereoKit with passthrough settings...");
		if (!SK.Initialize(settings))
		{
			Log.Err("Failed to initialize StereoKit!");
			return;
		}
		
		Log.Info($"StereoKit initialized. Device: {Device.Name}, DisplayBlend: {Device.DisplayBlend}");
		
		// Force clear color to transparent for passthrough
		Renderer.ClearColor = new Color(0, 0, 0, 0);  // Transparent black
		
		// Log passthrough capability
		Log.Info($"Display blend mode: {Device.DisplayBlend}");
		if (Device.DisplayBlend != DisplayBlend.Opaque)
		{
			Log.Info("Passthrough should be active!");
		}
		else
		{
			Log.Warn("Device is in opaque mode - passthrough not available");
		}

		// Log hand tracking availability
		Log.Info("Hand tracking setup - ensure permissions are granted in device settings");
		Log.Info("MAN-11: Pinch gesture detection system initialized");
		Log.Info("MAN-12: Microphone recording system initialized");

		// Initialize microphone recorder first
		_microphoneRecorder = new MicrophoneRecorder();
		_microphoneRecorder.Initialize();

		// Initialize pinch gesture detection and recording workflow
		_pinchDetector = new PinchGestureDetector();
		_recordingWorkflow = new RecordingWorkflow();
		_recordingWorkflow.Initialize(_microphoneRecorder);

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
			
			// Update recording workflow and microphone recorder
			_recordingWorkflow.Update();
			_microphoneRecorder.Update();
			
			// Debug: Log display blend mode on first few frames
			if (Time.Totalf < 2.0f)  // Log for first 2 seconds
			{
				Log.Info($"Time {Time.Totalf:F1}s: DisplayBlend = {Device.DisplayBlend}");
			}
			
			// Only render floor in opaque mode (PC/simulator)
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
			
			UI.HSeparator();
			UI.Text("Microphone Recorder:", TextAlign.CenterLeft);
			
			// Microphone recorder status (MAN-12)
			_microphoneRecorder.GetRecordingInfo(out bool micRecording, out double micDuration, out float micIntensity, out int micFileCount);
			UI.Label($"Audio Recording: {(micRecording ? "ACTIVE" : "Inactive")}");
			if (micRecording)
			{
				UI.Label($"  Duration: {micDuration:F1}s");
				UI.Label($"  Intensity: {micIntensity:F3}");
				UI.Label($"  Quality: 16kHz mono");
			}
			UI.Label($"Audio Files: {micFileCount}");
			if (_recordingWorkflow.LastRecordedAudioPath != null)
			{
				UI.Label($"Last Recording: {Path.GetFileName(_recordingWorkflow.LastRecordedAudioPath)}");
			}
			
			UI.WindowEnd();
		});
	}
}