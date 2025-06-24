using StereoKit;
using System;

namespace StereoKitQuest3;

class Program
{
	static void Main(string[] args)
	{
		var app = new App();
		
		try
		{
			app.Initialize();
			app.Run();
		}
		catch (Exception ex)
		{
			Log.Err($"Application error: {ex.Message}");
		}
	}
}