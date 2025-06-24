using StereoKit;
using StereoKitQuest3.Gestures;
using System;
using System.Collections.Generic;

namespace StereoKitQuest3.Workflows;

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