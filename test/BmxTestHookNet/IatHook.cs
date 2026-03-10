using System.Runtime.InteropServices;

namespace BmxTestHookNet;

internal static unsafe class IatHook {
	private static nint s_originalWriteConsoleW;
	private static nint s_originalWriteFile;
	private static nint s_originalReadConsoleW;
	private static nint s_originalReadConsoleInputW;
	private static nint s_originalReadFile;

	private static nint* s_iatEntryWriteConsoleW;
	private static nint* s_iatEntryReadConsoleW;
	private static nint* s_iatEntryReadConsoleInputW;

	private const int MaxMultiEntries = 8;
	private static readonly nint*[] s_iatEntriesWriteFile = new nint*[MaxMultiEntries];
	private static int s_iatEntriesWriteFileCount;
	private static readonly nint*[] s_iatEntriesReadFile = new nint*[MaxMultiEntries];
	private static int s_iatEntriesReadFileCount;

	// Console handles
	private static nint s_stdoutHandle;
	private static nint s_stderrHandle;
	private static nint s_stdinHandle;

	[UnmanagedFunctionPointer( CallingConvention.StdCall, CharSet = CharSet.Unicode )]
	private delegate bool WriteConsoleWDelegate(
		nint hConsoleOutput,
		char* lpBuffer,
		uint nNumberOfCharsToWrite,
		uint* lpNumberOfCharsWritten,
		nint lpReserved
	);

	[UnmanagedFunctionPointer( CallingConvention.StdCall )]
	private delegate bool WriteFileDelegate(
		nint hFile,
		byte* lpBuffer,
		uint nNumberOfBytesToWrite,
		uint* lpNumberOfBytesWritten,
		nint lpOverlapped
	);

	[UnmanagedFunctionPointer( CallingConvention.StdCall, CharSet = CharSet.Unicode )]
	private delegate bool ReadConsoleWDelegate(
		nint hConsoleInput,
		char* lpBuffer,
		uint nNumberOfCharsToRead,
		uint* lpNumberOfCharsRead,
		nint pInputControl
	);

	[UnmanagedFunctionPointer( CallingConvention.StdCall )]
	private delegate bool ReadConsoleInputWDelegate(
		nint hConsoleInput,
		NativeMethods.INPUT_RECORD* lpBuffer,
		uint nLength,
		uint* lpNumberOfEventsRead
	);

	[UnmanagedFunctionPointer( CallingConvention.StdCall )]
	private delegate bool ReadFileDelegate(
		nint hFile,
		byte* lpBuffer,
		uint nNumberOfBytesToRead,
		uint* lpNumberOfBytesRead,
		nint lpOverlapped
	);

	// Prevents GC from collecting delegates
	private static WriteConsoleWDelegate? s_hookWriteConsoleW;
	private static WriteFileDelegate? s_hookWriteFile;
	private static ReadConsoleWDelegate? s_hookReadConsoleW;
	private static ReadConsoleInputWDelegate? s_hookReadConsoleInputW;
	private static ReadFileDelegate? s_hookReadFile;

	// State for sending in key events
	private static string s_pendingKeys = "";
	private static int s_pendingKeyIndex;

	public static void InstallAll() {
		nint kernel32 = NativeMethods.GetModuleHandleW( "kernel32.dll" );
		if( kernel32 == 0 ) {
			DebugLog.Log( "ERROR: Could not get kernel32.dll handle" );
			return;
		}

		s_originalWriteConsoleW = NativeMethods.GetProcAddress( kernel32, "WriteConsoleW" );
		s_originalWriteFile = NativeMethods.GetProcAddress( kernel32, "WriteFile" );
		s_originalReadConsoleW = NativeMethods.GetProcAddress( kernel32, "ReadConsoleW" );
		s_originalReadConsoleInputW = NativeMethods.GetProcAddress( kernel32, "ReadConsoleInputW" );
		s_originalReadFile = NativeMethods.GetProcAddress( kernel32, "ReadFile" );

		if( s_originalWriteConsoleW == 0 || s_originalWriteFile == 0
			|| s_originalReadConsoleW == 0 || s_originalReadConsoleInputW == 0
			|| s_originalReadFile == 0 ) {
			DebugLog.Log( "ERROR: Could not resolve one or more function addresses" );
			return;
		}

		// Cache standard handles
		s_stdoutHandle = NativeMethods.GetStdHandle( NativeMethods.STD_OUTPUT_HANDLE );
		s_stderrHandle = NativeMethods.GetStdHandle( NativeMethods.STD_ERROR_HANDLE );
		s_stdinHandle = NativeMethods.GetStdHandle( NativeMethods.STD_INPUT_HANDLE );

		/*
		DebugLog.Log( $"Resolved WriteConsoleW @ 0x{s_originalWriteConsoleW:X}" );
		DebugLog.Log( $"Resolved WriteFile     @ 0x{s_originalWriteFile:X}" );
		DebugLog.Log( $"Resolved ReadConsoleW  @ 0x{s_originalReadConsoleW:X}" );
		DebugLog.Log( $"Resolved ReadConsoleInputW @ 0x{s_originalReadConsoleInputW:X}" );
		DebugLog.Log( $"Resolved ReadFile      @ 0x{s_originalReadFile:X}" );
		DebugLog.Log( $"stdin=0x{s_stdinHandle:X} stdout=0x{s_stdoutHandle:X} stderr=0x{s_stderrHandle:X}" );
		*/

		// Create hook delegates
		s_hookWriteConsoleW = HookWriteConsoleW;
		s_hookWriteFile = HookWriteFile;
		s_hookReadConsoleW = HookReadConsoleW;
		s_hookReadConsoleInputW = HookReadConsoleInputW;
		s_hookReadFile = HookReadFile;

		nint hookWriteConsoleWPtr = Marshal.GetFunctionPointerForDelegate( s_hookWriteConsoleW );
		nint hookWriteFilePtr = Marshal.GetFunctionPointerForDelegate( s_hookWriteFile );
		nint hookReadConsoleWPtr = Marshal.GetFunctionPointerForDelegate( s_hookReadConsoleW );
		nint hookReadConsoleInputWPtr = Marshal.GetFunctionPointerForDelegate( s_hookReadConsoleInputW );
		nint hookReadFilePtr = Marshal.GetFunctionPointerForDelegate( s_hookReadFile );

		// Walk the IAT of the main exe module and patch entries
		nint exeBase = NativeMethods.GetModuleHandleW( null );
		if( exeBase == 0 ) {
			DebugLog.Log( "ERROR: Could not get exe module handle" );
			return;
		}

		int patchCount = PatchIat(
			exeBase,
			hookWriteConsoleWPtr,
			hookWriteFilePtr,
			hookReadConsoleWPtr,
			hookReadConsoleInputWPtr,
			hookReadFilePtr
		);

		//DebugLog.Log( $"IAT patching complete — {patchCount} entries patched" );
	}

	public static void RemoveAll() {
		RestoreIatEntry( s_iatEntryWriteConsoleW, s_originalWriteConsoleW, "WriteConsoleW" );
		RestoreIatEntry( s_iatEntryReadConsoleW, s_originalReadConsoleW, "ReadConsoleW" );
		RestoreIatEntry( s_iatEntryReadConsoleInputW, s_originalReadConsoleInputW, "ReadConsoleInputW" );

		for( int i = 0; i < s_iatEntriesWriteFileCount; i++ ) {
			RestoreIatEntry( s_iatEntriesWriteFile[i], s_originalWriteFile, $"WriteFile[{i}]" );
		}
		for( int i = 0; i < s_iatEntriesReadFileCount; i++ ) {
			RestoreIatEntry( s_iatEntriesReadFile[i], s_originalReadFile, $"ReadFile[{i}]" );
		}

		s_hookWriteConsoleW = null;
		s_hookWriteFile = null;
		s_hookReadConsoleW = null;
		s_hookReadConsoleInputW = null;
		s_hookReadFile = null;
	}

	private static int PatchIat(
		nint moduleBase,
		nint hookWriteConsoleW,
		nint hookWriteFile,
		nint hookReadConsoleW,
		nint hookReadConsoleInputW,
		nint hookReadFile
	) {
		int patched = 0;

		var dosHeader = (NativeMethods.IMAGE_DOS_HEADER*)moduleBase;
		if( dosHeader->e_magic != 0x5A4D ) {
			DebugLog.Log( "ERROR: Invalid DOS header" );
			return 0;
		}

		byte* peHeader = (byte*)moduleBase + dosHeader->e_lfanew;
		uint peSignature = *(uint*)peHeader;
		if( peSignature != 0x00004550 ) {
			DebugLog.Log( "ERROR: Invalid PE signature" );
			return 0;
		}

		byte* optionalHeader = peHeader + 4 + 20;
		ushort magic = *(ushort*)optionalHeader;
		if( magic != 0x20B ) {
			DebugLog.Log( $"ERROR: Unsupported PE format (magic=0x{magic:X})" );
			return 0;
		}

		uint importDirRva = *(uint*)( optionalHeader + 120 );
		if( importDirRva == 0 ) {
			DebugLog.Log( "No import directory found" );
			return 0;
		}

		var importDesc = (NativeMethods.IMAGE_IMPORT_DESCRIPTOR*)( (byte*)moduleBase + importDirRva );

		while( importDesc->Name != 0 ) {
			if( importDesc->FirstThunk != 0 ) {
				nint* iatSlot = (nint*)( (byte*)moduleBase + importDesc->FirstThunk );

				while( *iatSlot != 0 ) {
					if( *iatSlot == s_originalWriteConsoleW ) {
						PatchIatSlot( iatSlot, hookWriteConsoleW );
						s_iatEntryWriteConsoleW = iatSlot;
						patched++;
						//DebugLog.Log( "Patched IAT: WriteConsoleW" );
					} else if( *iatSlot == s_originalWriteFile ) {
						PatchIatSlot( iatSlot, hookWriteFile );
						if( s_iatEntriesWriteFileCount < MaxMultiEntries ) {
							s_iatEntriesWriteFile[s_iatEntriesWriteFileCount++] = iatSlot;
						}
						patched++;
						//DebugLog.Log( $"Patched IAT: WriteFile[{s_iatEntriesWriteFileCount - 1}]" );
					} else if( *iatSlot == s_originalReadConsoleW ) {
						PatchIatSlot( iatSlot, hookReadConsoleW );
						s_iatEntryReadConsoleW = iatSlot;
						patched++;
						//DebugLog.Log( "Patched IAT: ReadConsoleW" );
					} else if( *iatSlot == s_originalReadConsoleInputW ) {
						PatchIatSlot( iatSlot, hookReadConsoleInputW );
						s_iatEntryReadConsoleInputW = iatSlot;
						patched++;
						//DebugLog.Log( "Patched IAT: ReadConsoleInputW" );
					} else if( *iatSlot == s_originalReadFile ) {
						PatchIatSlot( iatSlot, hookReadFile );
						if( s_iatEntriesReadFileCount < MaxMultiEntries ) {
							s_iatEntriesReadFile[s_iatEntriesReadFileCount++] = iatSlot;
						}
						patched++;
						//DebugLog.Log( $"Patched IAT: ReadFile[{s_iatEntriesReadFileCount - 1}]" );
					}

					iatSlot++;
				}
			}

			importDesc++;
		}

		return patched;
	}

	private static void PatchIatSlot( nint* slot, nint newValue ) {
		NativeMethods.VirtualProtect(
			(nint)slot,
			(nuint)sizeof( nint ),
			NativeMethods.PAGE_READWRITE,
			out uint oldProtect
		);

		*slot = newValue;

		NativeMethods.VirtualProtect(
			(nint)slot,
			(nuint)sizeof( nint ),
			oldProtect,
			out _
		);
	}

	private static void RestoreIatEntry( nint* iatSlot, nint originalValue, string name ) {
		if( iatSlot is null || originalValue == 0 ) return;

		PatchIatSlot( iatSlot, originalValue );
		DebugLog.Log( $"Restored IAT: {name}" );
	}

	private static bool HookWriteConsoleW(
		nint hConsoleOutput,
		char* lpBuffer,
		uint nNumberOfCharsToWrite,
		uint* lpNumberOfCharsWritten,
		nint lpReserved
	) {
		if( lpBuffer is not null && nNumberOfCharsToWrite > 0 ) {
			var text = new string( lpBuffer, 0, (int)nNumberOfCharsToWrite );
			PromptDetector.OnWrite( text );
			OutputCapture.OnWriteText( hConsoleOutput, text );
		}

		var original = (delegate* unmanaged[Stdcall]<nint, char*, uint, uint*, nint, int>)s_originalWriteConsoleW;
		return original( hConsoleOutput, lpBuffer, nNumberOfCharsToWrite, lpNumberOfCharsWritten, lpReserved ) != 0;
	}

	private static bool HookWriteFile(
		nint hFile,
		byte* lpBuffer,
		uint nNumberOfBytesToWrite,
		uint* lpNumberOfBytesWritten,
		nint lpOverlapped
	) {
		if( lpBuffer is not null && nNumberOfBytesToWrite > 0
			&& ( hFile == s_stdoutHandle || hFile == s_stderrHandle ) ) {
			// Decode bytes to string. NativeAOT with InvariantGlobalization uses UTF-8.
			string text;
			try {
				text = System.Text.Encoding.UTF8.GetString( lpBuffer, (int)nNumberOfBytesToWrite );
			} catch {
				text = "";
			}

			if( text.Length > 0 ) {
				PromptDetector.OnWrite( text );
				OutputCapture.OnWriteText( hFile, text );
			}
		}

		var original = (delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, nint, int>)s_originalWriteFile;
		return original( hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped ) != 0;
	}

	private static bool HookReadConsoleW(
		nint hConsoleInput,
		char* lpBuffer,
		uint nNumberOfCharsToRead,
		uint* lpNumberOfCharsRead,
		nint pInputControl
	) {
		PromptKind prompt = PromptDetector.Consume();

		if( prompt != PromptKind.Unknown ) {
			string response = CredentialProvider.GetResponse( prompt );

			// Always inject — even empty response means "press Enter"
			string fullResponse = response + "\r\n";
			int charsToCopy = Math.Min( fullResponse.Length, (int)nNumberOfCharsToRead );

			for( int i = 0; i < charsToCopy; i++ ) {
				lpBuffer[i] = fullResponse[i];
			}

			if( lpNumberOfCharsRead is not null ) {
				*lpNumberOfCharsRead = (uint)charsToCopy;
			}

			DebugLog.Log( $"ReadConsoleW: injected for {prompt} ({charsToCopy} chars)" );
			return true;
		}

		var original = (delegate* unmanaged[Stdcall]<nint, char*, uint, uint*, nint, int>)s_originalReadConsoleW;
		return original( hConsoleInput, lpBuffer, nNumberOfCharsToRead, lpNumberOfCharsRead, pInputControl ) != 0;
	}

	private static bool HookReadConsoleInputW(
		nint hConsoleInput,
		NativeMethods.INPUT_RECORD* lpBuffer,
		uint nLength,
		uint* lpNumberOfEventsRead
	) {
		// Check if we need to start feeding characters
		if( s_pendingKeys.Length == 0 || s_pendingKeyIndex >= s_pendingKeys.Length ) {
			PromptKind prompt = PromptDetector.Peek();
			if( prompt is PromptKind.Password or PromptKind.MfaResponse ) {
				string response = CredentialProvider.GetResponse( prompt );
				// Always inject — even empty password sends Enter to terminate
				s_pendingKeys = response + "\r";
				s_pendingKeyIndex = 0;
				PromptDetector.Consume();
				DebugLog.Log( $"ReadConsoleInputW: starting injection for {prompt} ({s_pendingKeys.Length} chars)" );
			}
		}

		// Feed one character at a time as a KEY_EVENT
		if( s_pendingKeyIndex < s_pendingKeys.Length && nLength >= 1 && lpBuffer is not null ) {
			char ch = s_pendingKeys[s_pendingKeyIndex];

			var rec = new NativeMethods.INPUT_RECORD();
			rec.EventType = NativeMethods.KEY_EVENT;
			rec.KeyEvent.bKeyDown = 1;
			rec.KeyEvent.wRepeatCount = 1;
			rec.KeyEvent.UnicodeChar = ch;
			if( ch == '\r' ) {
				rec.KeyEvent.wVirtualKeyCode = NativeMethods.VK_RETURN;
				rec.KeyEvent.wVirtualScanCode = 0x1C;
			}

			lpBuffer[0] = rec;
			if( lpNumberOfEventsRead is not null ) {
				*lpNumberOfEventsRead = 1;
			}

			s_pendingKeyIndex++;

			if( s_pendingKeyIndex >= s_pendingKeys.Length ) {
				s_pendingKeys = "";
				s_pendingKeyIndex = 0;
				DebugLog.Log( "ReadConsoleInputW: injection complete" );
			}

			return true;
		}

		var original = (delegate* unmanaged[Stdcall]<
			nint, NativeMethods.INPUT_RECORD*, uint, uint*, int
		>)s_originalReadConsoleInputW;
		return original( hConsoleInput, lpBuffer, nLength, lpNumberOfEventsRead ) != 0;
	}

	private static bool HookReadFile(
		nint hFile,
		byte* lpBuffer,
		uint nNumberOfBytesToRead,
		uint* lpNumberOfBytesRead,
		nint lpOverlapped
	) {
		if( hFile == s_stdinHandle ) {
			PromptKind prompt = PromptDetector.Consume();

			if( prompt != PromptKind.Unknown ) {
				string response = CredentialProvider.GetResponse( prompt );

				// Always inject — even empty = just press Enter
				string fullResponse = response + "\r\n";
				byte[] encoded = System.Text.Encoding.UTF8.GetBytes( fullResponse );
				int bytesToCopy = Math.Min( encoded.Length, (int)nNumberOfBytesToRead );

				fixed( byte* pEncoded = encoded ) {
					Buffer.MemoryCopy( pEncoded, lpBuffer, nNumberOfBytesToRead, bytesToCopy );
				}

				if( lpNumberOfBytesRead is not null ) {
					*lpNumberOfBytesRead = (uint)bytesToCopy;
				}

				DebugLog.Log( $"ReadFile(stdin): injected for {prompt} ({bytesToCopy} bytes)" );
				return true;
			}
		}

		var original = (delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, nint, int>)s_originalReadFile;
		return original( hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped ) != 0;
	}
}
