using System.Collections.Generic;
using UnityEngine;
using NAudio.Wave;

namespace Knit.LipSync.Editor
{
	[System.Serializable]
	public sealed class Sequence
	{
		public static Sequence CreateFromWaveFile( string wavePath, int frameRate, System.Action<UnityEngine.LogType, string> callback)
		{
			var reader = new WaveFileReader( wavePath);
			int frequency = reader.WaveFormat.SampleRate;
			int channels = reader.WaveFormat.Channels;
			int sampleSize = frequency / frameRate;
			
			if( channels > 2)
			{
				callback?.Invoke( LogType.Error, System.IO.Path.GetFileNameWithoutExtension( wavePath) +
					": Cannot process phonemes from an audio clip with " +
					"more than 2 channels");
				return null;
			}
			if( Engine.Initialize( frequency, sampleSize) != Engine.Result.Success)
			{
				callback?.Invoke( LogType.Error, "Could not create Lip Sync engine.");
				return null;
			}
			uint context = 0;
			Engine.Result result = Engine.CreateContext( ref context, Engine.ContextProviders.Enhanced);
			
			if( result != Engine.Result.Success)
			{
				callback?.Invoke( LogType.Error, "Could not create Phoneme context. (" + result + ")");
				Engine.Shutdown();
				return null;
			}
			sampleSize *= channels;
			var samples = new float[ sampleSize];
			var frames = new List<Frame>();
			
			reader.Position = 0;
			float[] samplesL = new float[ reader.SampleCount];
			float[] samplesR = null;
			
			if( channels == 2)
			{
				samplesR = new float[ reader.SampleCount];
			}
			int frameCount = 0;
			float[] buffer;
			
			while( (buffer = reader.ReadNextSampleFrame()) != null)
			{
				for( int i0 = 0; i0 < buffer.Length; ++i0)
				{
					switch( i0)
					{
						case 0:
						{
							samplesL[ frameCount] = buffer[ i0];
							break;
						}
						case 1:
						{
							samplesR[ frameCount] = buffer[ i0];
							break;
						}
					}
				}
				++frameCount;
			}
			int totalSamples = samplesL.Length * channels;
			callback?.Invoke( LogType.Log, $"frequency={frequency}, samples={totalSamples}, ch={channels}, seek={sampleSize}");
			frameCount = 0;
			
			for( int i0 = 0; i0 < totalSamples; i0 += sampleSize)
			{
				int remainingSamples = totalSamples - i0;
				
				if( remainingSamples >= sampleSize)
				{
					for( int i1 = 0; i1 < sampleSize; ++i1, ++frameCount)
					{
						samples[i1] = samplesL[ frameCount];
						if( channels == 2)
						{
							++i1;
							samples[i1] = samplesR[ frameCount];
						}
					}
				}
				else if( remainingSamples > 0)
				{
					var samples_clip = new float[ remainingSamples];
					
					for( int i1 = 0; i1 < remainingSamples; ++i1, ++frameCount)
					{
						samples_clip[ i1] = samplesL[ frameCount];
						
						if( channels == 2)
						{
							samples_clip[ ++i1] = samplesR[ frameCount];
						}
					}
					System.Array.Copy( samples_clip, samples, samples_clip.Length);
					System.Array.Clear( samples, samples_clip.Length, samples.Length - samples_clip.Length);
				}
				else
				{
					System.Array.Clear( samples, 0, samples.Length);
				}
				var frame = new Frame();
				Engine.ProcessFrame( context, samples, frame, channels == 2);
				frame.frameDelay = Mathf.CeilToInt( i0 / channels / (float)frequency * 1000.0f);
				frames.Add( frame);
			}
			callback?.Invoke( LogType.Log, 
				System.IO.Path.GetFileNameWithoutExtension( wavePath) + 
				" produced " + frames.Count +
				" viseme frames, playback rate is " + frameRate +
				" fps");
			Engine.DestroyContext( context);
			Engine.Shutdown();
			
			return new Sequence( frames, (float)reader.TotalTime.TotalSeconds);
		}
		public IEnumerable<Frame> Entries
		{
			get{ return m_Entries; }
		}
		public float Duration
		{
			get{ return m_Duration; }
		}
		Sequence( List<Frame> entries, float duration)
		{
			m_Entries = entries;
			m_Duration = duration;
		}
		[System.Serializable]
		public sealed class Frame
		{
			public void CopyInput( Frame input)
			{
				frameNumber = input.frameNumber;
				frameDelay = input.frameDelay;
				input.Visemes.CopyTo( Visemes, 0);
				laughterScore = input.laughterScore;
			}
			public void Reset()
			{
				frameNumber = 0;
				frameDelay = 0;
				System.Array.Clear( Visemes, 0, Engine.kVisemeCount);
				laughterScore = 0;
			}
			[SerializeField]
			public int frameNumber;										// count from start of recognition
			[SerializeField]
			public int frameDelay;										// in ms
			[SerializeField]
			public float[] Visemes = new float[ Engine.kVisemeCount];	// Array of floats for viseme frame. Size of Viseme Count, above
			[SerializeField]
			public float laughterScore;									// probability of laughter presence.
		};
		[SerializeField]
		List<Frame> m_Entries = new();
		[SerializeField]
		float m_Duration;
	};
}
