using UnityEngine;
using System.Collections.Generic;

namespace Knit.LipSync.Editor
{
	[System.Serializable]
	public sealed class Status
	{
		public static Status GetStateFromSequence( Sequence sequence)
		{
			var state = new Status();
			
			foreach( var entry in sequence.Entries)
			{
				float opened = 0;
				float closed = 0;
				float maxViseme = 0;
				int maxCaliber = 0;
				
				for( int i0 = 0; i0 < entry.Visemes.Length; ++i0)
				{
					var viseme = entry.Visemes[ i0];
					
					if( maxViseme < viseme)
					{
						maxViseme = viseme;
						maxCaliber = i0;
					}
					if( kMouthShapeOpen[ i0] != false)
					{
						opened += viseme;
					}
					else
					{
						closed += viseme;
					}
				}
				state.m_Entries.Add( new Frame( entry.frameDelay, (opened < closed)? 0 : 1, maxCaliber));
			}
			return state;
		}
		[System.Serializable]
		public sealed class Frame
		{
			public Frame( long time, float open, int caliber)
			{
				m_Time = time;
				m_Open = open;
				m_Caliber = caliber;
			}
			public long Time
			{
				get{ return m_Time; }
			}
			public float Open
			{
				get{ return m_Open; }
			}
			public int Caliber
			{
				get{ return m_Caliber; }
			}
			[SerializeField]
			long m_Time;
			[SerializeField]
			float m_Open;
			[SerializeField]
			int m_Caliber;
		}
		public IReadOnlyList<Frame> Entries
		{
			get{ return m_Entries; }
		}
		/* https://developer.oculus.com/documentation/unity/audio-ovrlipsync-viseme-reference/ */
		public static readonly bool[] kMouthShapeOpen =
		{
			false,	//sil
			false,	//PP
			false,	//FF
			false,	//TH
			true,	//DD
			true,	//kk
			true,	//CH
			false,	//SS
			true,	//nn
			false,	//RR
			true,	//aa
			true,	//E
			true,	//ih
			true,	//oh
			false,	//ou
		};
		[SerializeField]
		List<Frame> m_Entries = new();
	};
}
