using System.Runtime.InteropServices;

namespace BmxTestHookNet;

internal static class DllMain {
	private const uint DLL_PROCESS_ATTACH = 1;
	private const uint DLL_PROCESS_DETACH = 0;

	[UnmanagedCallersOnly(EntryPoint = "DllMain")]
	public static int Main( nint hModule, uint reason, nint reserved ) {
		switch( reason ) {
			case DLL_PROCESS_ATTACH:
				CredentialProvider.Init();
				OutputCapture.Init();
				IatHook.InstallAll();
				break;

			case DLL_PROCESS_DETACH:
				IatHook.RemoveAll();
				OutputCapture.Shutdown();
				break;
		}
		return 1;
	}
}
