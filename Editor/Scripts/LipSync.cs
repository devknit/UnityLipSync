
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.InteropServices;

namespace Knit.LipSync.Editor
{
	public enum CreateLocation
	{
		LipSyncDirectory = 1 << 0,
		CurrentDirectory = 1 << 1,
		[HideInInspector]
		MainAsset = LipSyncDirectory | CurrentDirectory,
		SubAsset = 1 << 2, // サブアセットには保存できない？
	}
	public static class Engine
	{
		public const int kFrameRate = 60;
		public const long kFrequency = 125; /* ミリ秒 */
		public static string kDirectoryName = "LipSyncs";
		
		public static bool Create( string waveAssetPath, CreateLocation location, int frameRate, long frequency, System.Action<LogType, string> callback)
		{
			if( AssetDatabase.GetMainAssetTypeAtPath( waveAssetPath) == typeof( AudioClip))
			{
				string waveAssetName = Path.GetFileNameWithoutExtension( waveAssetPath);
				SingleCurve curveAsset = null;
				
				if( (location & CreateLocation.MainAsset) != 0)
				{
					string directory = Path.GetDirectoryName( waveAssetPath).Replace( '\\', '/');
					
					if( location == CreateLocation.LipSyncDirectory)
					{
						directory = Path.Combine( directory, kDirectoryName).Replace( '\\', '/');
					}
					if( Directory.Exists( directory) == false)
					{
						Directory.CreateDirectory( directory);
					}
					
					string curveAssetPath = Path.Combine( directory, Path.ChangeExtension( waveAssetName, ".asset")).Replace( '\\', '/');
					curveAsset = AssetDatabase.LoadAssetAtPath<SingleCurve>( curveAssetPath);
					
					if( curveAsset == null)
					{
						curveAsset = ScriptableObject.CreateInstance<SingleCurve>();
						AssetDatabase.CreateAsset( curveAsset, curveAssetPath);
					}
				}
				else if( location == CreateLocation.SubAsset)
				{
					Object[] objects = AssetDatabase.LoadAllAssetsAtPath( waveAssetPath);
					
					for( int i0 = 0; i0 < objects.Length; ++i0)
					{
						if( objects[ i0] is SingleCurve curve && AssetDatabase.IsSubAsset( curve) != false)
						{
							if( curve.name == waveAssetName)
							{
								curveAsset = curve;
								break;
							}
						}
					}
					if( curveAsset == null)
					{
						curveAsset = ScriptableObject.CreateInstance<SingleCurve>();
						AssetDatabase.AddObjectToAsset( curveAsset, waveAssetPath);
						AssetDatabase.ImportAsset( waveAssetPath);
					}
				}
				if( curveAsset != null)
				{
					Sequence sequence = Sequence.CreateFromWaveFile( waveAssetPath, frameRate, callback);
					
					if( sequence != null)
					{
						Status status = Status.GetStateFromSequence( sequence);
						Curve curve = Curve.GetCurveFromState( status, frequency);
						var animationCurve = new AnimationCurve();
						
						// CreateJson( waveAssetPath, "sequence", sequence);
						// CreateJson( waveAssetPath, "status", status);
						// CreateJson( waveAssetPath, "curve", curve);
						
						foreach( var entry in curve.Entries)
						{
							animationCurve.AddKey( new Keyframe( entry.Time * 0.001f, entry.Open, 0, 0, 0, 0));
						}
						curveAsset.SetAnimationCurve( AssetDatabase.LoadAssetAtPath<AudioClip>( waveAssetPath), animationCurve);
						EditorUtility.SetDirty( curveAsset);
						return true;
					}
				}
			}
			return false;
		}
		[MenuItem( "Assets/Create/LipSync/Current Directory", true, 220)]
		static bool Validate()
		{
			for( int i0 = 0; i0 < Selection.assetGUIDs.Length; ++i0)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( Selection.assetGUIDs[ i0]);
				
				if( string.IsNullOrEmpty( assetPath) == false)
				{
					if( AssetDatabase.GetMainAssetTypeAtPath( assetPath) == typeof( AudioClip))
					{
						return true;
					}
				}
			}
			return false;
		}
		[MenuItem( "Assets/Create/LipSync/Current Directory", false, 220)]
		static void Create()
		{
			bool bSaveAsset = false;
			
			for( int i0 = 0; i0 < Selection.assetGUIDs.Length; ++i0)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( Selection.assetGUIDs[ i0]);
				
				if( Create( assetPath, 
					CreateLocation.CurrentDirectory, kFrameRate, kFrequency,
					(LogType logType, string message) =>
					{
						Debug.LogFormat( logType, LogOption.None, null, message, string.Empty);
					}) != false)
				{
					bSaveAsset = true;
				}
			}
			if( bSaveAsset != false)
			{
				AssetDatabase.SaveAssets();
			}
		}
		static void CreateJson( string assetPath, string name, object jsonObject)
		{
			string filePath = assetPath.Replace( Path.GetExtension( assetPath), $"_{name}.json");
			File.WriteAllText( filePath, JsonUtility.ToJson( jsonObject, true));
			AssetDatabase.ImportAsset( filePath);
		}
		internal static Result Initialize()
		{
			int sampleRate = AudioSettings.outputSampleRate;
			AudioSettings.GetDSPBufferSize( out int bufferSize, out int numbuf);
			return (Result)ovrLipSyncDll_Initialize( sampleRate, bufferSize);
		}
		internal static Result Initialize( int sampleRate, int bufferSize)
		{
			return (Result)ovrLipSyncDll_Initialize( sampleRate, bufferSize);
		}
		internal static void Shutdown()
		{
			ovrLipSyncDll_Shutdown();
		}
		/// <summary>
		/// Creates a lip-sync context.
		/// </summary>
		/// <returns>error code</returns>
		/// <param name="context">Context.</param>
		/// <param name="provider">Provider.</param>
		/// <param name="enableAcceleration">Enable DSP Acceleration.</param>
		internal static Result CreateContext(
			ref uint context,
			ContextProviders provider,
			int sampleRate = 0,
			bool enableAcceleration = false)
		{
			if( Initialize() != Result.Success)
			{
				return Result.CannotCreateContext;
			}
			return (Result)ovrLipSyncDll_CreateContextEx( ref context, provider, sampleRate, enableAcceleration);
		}
		/// <summary>
		/// Destroy a lip-sync context.
		/// </summary>
		/// <returns>The context.</returns>
		/// <param name="context">Context.</param>
		internal static Result DestroyContext(uint context)
		{
			return (Result)ovrLipSyncDll_DestroyContext( context);
		}
		/// <summary>
		///  Process float[] audio buffer by lip-sync engine.
		/// </summary>
		/// <returns>error code</returns>
		/// <param name="context">Context.</param>
		/// <param name="audioBuffer"> PCM audio buffer.</param>
		/// <param name="frame">Lip-sync Frame.</param>
		/// <param name="stereo">Whether buffer is part of stereo or mono stream.</param>
		internal static Result ProcessFrame( uint context, 
			float[] audioBuffer, Sequence.Frame frame, bool stereo)
		{
			var dataType = (stereo != false)? AudioDataType.F32_Stereo : AudioDataType.F32_Mono;
			var numSamples = (uint)(stereo ? audioBuffer.Length / 2 : audioBuffer.Length);
			var handle = GCHandle.Alloc( audioBuffer, GCHandleType.Pinned);
			var rc = ovrLipSyncDll_ProcessFrameEx( context,
				handle.AddrOfPinnedObject(), numSamples, dataType,
				ref frame.frameNumber, ref frame.frameDelay,
				frame.Visemes, frame.Visemes.Length,
				ref frame.laughterScore,
				null, 0);
			handle.Free();
			return (Result)rc;
		}
		[DllImport( kOVRLS)]
		static extern int ovrLipSyncDll_Initialize( int samplerate, int buffersize);
		[DllImport( kOVRLS)]
		static extern void ovrLipSyncDll_Shutdown();
		[DllImport( kOVRLS)]
		static extern int ovrLipSyncDll_CreateContextEx( 
			ref uint context,
			ContextProviders provider,
			int sampleRate,
			bool enableAcceleration);
		[DllImport( kOVRLS)]
		static extern int ovrLipSyncDll_DestroyContext( uint context);
		[DllImport( kOVRLS)]
		static extern int ovrLipSyncDll_ProcessFrameEx(
			uint context,
			System.IntPtr audioBuffer,
			uint bufferSize,
			AudioDataType dataType,
			ref int frameNumber,
			ref int frameDelay,
			float[] visemes,
			int visemeCount,
			ref float laughterScore,
			float[] laughterCategories,
			int laughterCategoriesLength);
		
		internal enum Result
		{
			Success = 0,
			Unknown = -2200,  //< An unknown error has occurred
			CannotCreateContext = -2201,  //< Unable to create a context
			InvalidParam = -2202,  //< An invalid parameter, e.g. NULL pointer or out of range
			BadSampleRate = -2203,  //< An unsupported sample rate was declared
			MissingDLL = -2204,  //< The DLL or shared library could not be found
			BadVersion = -2205,  //< Mismatched versions between header and libs
			UndefinedFunction = -2206   //< An undefined function
		};
		internal enum AudioDataType
		{
			// Signed 16-bit integer mono audio stream
			S16_Mono,
			// Signed 16-bit integer stereo audio stream
			S16_Stereo,
			// Signed 32-bit float mono audio stream
			F32_Mono,
			// Signed 32-bit float stereo audio stream
			F32_Stereo
		};
		internal enum ContextProviders
		{
			Original,
			Enhanced,
			Enhanced_with_Laughter,
		};
		// Various visemes
		internal enum Viseme
		{
			sil,
			PP,
			FF,
			TH,
			DD,
			kk,
			CH,
			SS,
			nn,
			RR,
			aa,
			E,
			ih,
			oh,
			ou
		};
		internal static readonly int kVisemeCount = System.Enum.GetNames( typeof( Viseme)).Length;
		const string kOVRLS = "OVRLipSync";
	}
}
