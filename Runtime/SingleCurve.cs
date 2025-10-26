
using UnityEngine;

namespace Knit.LipSync
{
	public class SingleCurve : ScriptableObject
	{
		public AnimationCurve Curve
		{
			get{ return m_Curve; }
		}
		public bool Evaluate( float time, out float value)
		{
			value = m_Curve.Evaluate( time);
			return time >= m_Curve.keys[ m_Curve.length - 1].time;
		}
	#if UNITY_EDITOR
		public void SetAnimationCurve( AudioClip audioClip, AnimationCurve animationCurve)
		{
			m_Clip = audioClip;
			m_Curve = animationCurve;
		}
		public AudioClip Clip
		{
			get{ return m_Clip; }
		}
		[SerializeField]
		AudioClip m_Clip;
	#endif
		[SerializeField]
		AnimationCurve m_Curve = new();
	};
}