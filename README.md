# StereoKit Quest 3 Test Project

A StereoKit application designed for Oculus Quest 3, featuring hand tracking, pinch gesture detection, and mixed reality capabilities.

## Features

- ✅ **Quest 3 Support**: Configured for Oculus Quest 3 with OpenXR
- ✅ **Hand Tracking**: Right and left hand detection with pinch gesture recognition
- ✅ **Mixed Reality**: Supports both VR and passthrough modes
- ✅ **Permissions**: Pre-configured with hand tracking, microphone, and camera permissions
- ✅ **Cross-Platform**: Builds for both desktop (testing) and Android (Quest 3)

## Prerequisites

- .NET 9.0 SDK
- Android workload for .NET (`dotnet workload install android`)
- For Quest 3 deployment:
  - Android SDK and ADB
  - Oculus Quest 3 with Developer Mode enabled
  - USB-C cable for device connection

## Building the Project

### Desktop Version (for testing)
```bash
dotnet build
```

### Android Version (for Quest 3)
```bash
dotnet build Projects/Android/StereoKitQuest3.Android.csproj
```

## Running on Quest 3

1. **Enable Developer Mode** on your Quest 3:
   - Install Oculus app on your phone
   - Create a developer organization
   - Enable Developer Mode in headset settings

2. **Install ADB** (Android Debug Bridge):
   ```bash
   # macOS with Homebrew
   brew install android-platform-tools
   ```

3. **Connect and Deploy**:
   ```bash
   # Connect Quest 3 via USB-C
   adb devices  # Verify device is connected
   
   # Deploy to Quest 3
   dotnet publish Projects/Android/StereoKitQuest3.Android.csproj -c Release
   adb install Projects/Android/bin/Release/net9.0-android/publish/com.companyname.stereokitquest3.apk
   ```

4. **Grant Permissions** on Quest 3:
   - Hand tracking permission
   - Microphone permission (for future audio recording)
   - Camera permission (for future photo capture)

## Project Structure

```
StereoKitTest/
├── Program.cs                              # Main application code
├── StereoKitQuest3.csproj                 # Desktop project file
├── Assets/
│   └── floor.hlsl                         # Floor shader
├── Platforms/
│   └── Android/
│       ├── AndroidManifest.xml           # Android permissions & config
│       └── MainActivity.cs               # Android entry point
└── Projects/
    └── Android/
        └── StereoKitQuest3.Android.csproj # Android build project
```

## Key Features in Code

### Hand Tracking Status Display
The app displays real-time hand tracking status and pinch detection:
- Right/Left hand tracking status
- Device information
- Pinch gesture detection (threshold: 0.5)

### Quest 3 Permissions
Pre-configured in `AndroidManifest.xml`:
- Hand tracking (`com.oculus.permission.HAND_TRACKING`)
- Microphone (`android.permission.RECORD_AUDIO`)
- Camera (`android.permission.CAMERA`)
- Passthrough features (`com.oculus.feature.PASSTHROUGH`)

### OpenXR Configuration
Optimized for Quest 3 with:
- OpenXR loader configuration
- Multi-platform VR runtime support
- Hand tracking V2.0 support

## Development Roadmap

This project serves as the foundation for:
1. **MAN-11**: Pinch gesture detection implementation
2. **MAN-12**: Microphone recording workflow
3. **MAN-13**: Camera picture capture
4. **MAN-14**: Gemini API integration
5. **MAN-15**: ElevenLabs TTS integration

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure Android workload is installed
   ```bash
   dotnet workload install android
   ```

2. **Hand Tracking Not Working**: 
   - Check Quest 3 settings for hand tracking
   - Ensure permissions are granted in device settings
   - Verify manifest includes hand tracking permissions

3. **Deployment Issues**:
   - Enable Developer Mode on Quest 3
   - Check ADB connection: `adb devices`
   - Allow USB debugging when prompted on headset

## Next Steps

1. Test on actual Quest 3 device
2. Implement pinch gesture workflow (MAN-11)
3. Add microphone recording capabilities (MAN-12)
4. Integrate camera capture (MAN-13)
5. Connect to Gemini and ElevenLabs APIs (MAN-14, MAN-15)

## License

This project is part of the StereoKitTest development suite.
