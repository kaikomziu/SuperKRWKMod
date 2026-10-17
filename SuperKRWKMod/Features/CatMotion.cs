using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// Applies additive position/rotation offsets to the cat's visuals. The game only scales this
	/// transform (petting), so offsets are removed and re-applied each frame without fighting it.
	/// </summary>
	public class CatMotion : MonoBehaviour
	{
		private Transform _target;
		private Vector3 _appliedOffset;
		private float _appliedAngle;

		public Vector2 ZoomiesOffset { get; set; }
		public float ZoomiesAngle { get; set; }
		public Vector2 SleepOffset { get; set; }

		public bool IsMoving => _appliedOffset != Vector3.zero || _appliedAngle != 0f;

		public void Attach(Transform target)
		{
			_target = target;
		}

		private void LateUpdate()
		{
			if (_target == null)
				return;

			var offset = (Vector3)(ZoomiesOffset + SleepOffset);
			var angle = ZoomiesAngle;

			if (offset != _appliedOffset)
			{
				_target.localPosition += offset - _appliedOffset;
				_appliedOffset = offset;
			}

			if (angle != _appliedAngle)
			{
				_target.localRotation *= Quaternion.Euler(0f, 0f, angle - _appliedAngle);
				_appliedAngle = angle;
			}
		}

		public void ResetMotion()
		{
			ZoomiesOffset = Vector2.zero;
			ZoomiesAngle = 0f;
			SleepOffset = Vector2.zero;
			LateUpdate();
		}

		private void OnDestroy()
		{
			ResetMotion();
		}
	}
}
