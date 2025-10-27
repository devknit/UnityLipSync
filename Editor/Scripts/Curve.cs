
using UnityEngine;
using System.Collections.Generic;

namespace Knit.LipSync.Editor
{
	[System.Serializable]
	public class Curve
	{
		public static Curve GetCurveFromState( Status status, long frequency)
		{
			var entries = status.Entries;
			var curve = new Curve();
			bool speaking = false;
			bool closingMouth = false;
			bool lastKeySpeaking = true;
			bool keepOpen = false;
			long lastKeyTime = 0;
			
			for( int i0 = 0; i0 < entries.Count; ++i0)
			{
				Status.Frame nextEntry = i0 + 1 < entries.Count ? entries[ i0 + 1] : null;
				var entry = entries[ i0];
				var frame = new Frame
				{
					Time = entry.Time
				};
				if( i0 == 0)
				{
					lastKeySpeaking = false;
					frame.Open = 0;
					curve.m_Entries.Add( frame);
				}
				else
				{
					if( speaking != false)
					{
						if(keepOpen != false)
						{
							if( nextEntry?.Caliber != entry.Caliber)
							{
								frame.Open = 1;
								curve.m_Entries.Add( frame);
								lastKeyTime = frame.Time;
								closingMouth = true;
								keepOpen = false;
							}
						}
						else if( entry.Time >= lastKeyTime + frequency)
						{
							if( closingMouth == false)
							{
								if( nextEntry?.Caliber == entry.Caliber && Status.kMouthShapeOpen[ entry.Caliber] == true)
								{
									frame.Open = 1;
									frame.Time = lastKeyTime + frequency;
									curve.m_Entries.Add( frame);
									keepOpen = true;
									lastKeyTime = frame.Time;
								}
								else
								{
									frame.Open = 1;
									closingMouth = true;
									frame.Time = lastKeyTime + frequency;
									curve.m_Entries.Add( frame);
									lastKeyTime = frame.Time;
								}
							}
							else
							{
								frame.Open = 0;
								frame.Time = lastKeyTime + frequency;
								curve.m_Entries.Add( frame);
								lastKeyTime = frame.Time;
								closingMouth = false;
								
								if(nextEntry?.Open == 0)
								{
									speaking = false;
									lastKeySpeaking = false;
								}
							}
						}
					}
					else
					{
						if( entry.Open == 1.0f)
						{
							if( lastKeySpeaking == false)
							{
								lastKeySpeaking = true;
								frame.Open = 0;
								curve.m_Entries.Add( frame);
								lastKeyTime = frame.Time;
								speaking = true;
								closingMouth = false;
							}
						}
						else
						{
							if( lastKeySpeaking != false)
							{
								lastKeySpeaking = false;
								frame.Open = 0;
								curve.m_Entries.Add( frame);
								lastKeyTime = frame.Time;
								speaking = false;
							}
						}
					}
				}
			}
			var lastEntry = curve.m_Entries[ ^1];
			
			if( speaking != false)
			{
				var endFrame = new Frame();
				
				if( lastEntry.Open < 1.0f)
				{
					endFrame.Open = 1.0f;
					endFrame.Time = lastEntry.Time + frequency;
					curve.m_Entries.Add(endFrame);
					
					var secondEndFrame = new Frame
					{
						Open = 0.0f,
						Time = endFrame.Time + frequency
					};
					curve.m_Entries.Add( secondEndFrame);
				}
				else
				{
					endFrame.Open = 0.0f;
					endFrame.Time = lastEntry.Time + frequency;
					curve.m_Entries.Add( endFrame);
				}
			}
			else if( lastEntry.Time < entries[ entries.Count - 1].Time)
			{
				curve.m_Entries.Add( new Frame( entries[ entries.Count - 1].Time, 0));
			}
			return curve;
		}
		[System.Serializable]
		public sealed class Frame
		{
			public Frame()
			{
			}
			public Frame( long time, float open)
			{
				m_Time = time;
				m_Open = open;
			}
			public long Time
			{
				get{ return m_Time; }
				internal set{ m_Time = value; }
			}
			public float Open
			{
				get{ return m_Open; }
				internal set{ m_Open = value; }
			}
			[SerializeField]
			long m_Time;
			[SerializeField]
			float m_Open; 
		}
		public IEnumerable<Frame> Entries
		{
			get{ return m_Entries; }
		}
		[SerializeField]
		List<Frame> m_Entries = new();
	};
}
