using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace BlastonCameraBehaviour
{
	public class Controller
	{
		public class ButtonMask
		{
			public const ulong System = (1ul << (int)EVRButtonId.k_EButton_System); // reserved
			public const ulong ApplicationMenu = (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu);
			public const ulong Grip = (1ul << (int)EVRButtonId.k_EButton_Grip);
			public const ulong Axis0 = (1ul << (int)EVRButtonId.k_EButton_Axis0);
			public const ulong Axis1 = (1ul << (int)EVRButtonId.k_EButton_Axis1);
			public const ulong Axis2 = (1ul << (int)EVRButtonId.k_EButton_Axis2);
			public const ulong Axis3 = (1ul << (int)EVRButtonId.k_EButton_Axis3);
			public const ulong Axis4 = (1ul << (int)EVRButtonId.k_EButton_Axis4);
			public const ulong Touchpad = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad);
			public const ulong Trigger = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Trigger);
		}

		public class Device
		{
			VRControllerState_t state;
			VRControllerState_t prevState;
			TrackedDevicePose_t pose;
			int prevFrameCount = -1;

			public SteamVR_Utils.RigidTransform transform { get { Update(); return new SteamVR_Utils.RigidTransform(pose.mDeviceToAbsoluteTracking); } }

			public Device(uint i) { deviceIndex = i; }
			public uint deviceIndex { get; private set; }
			public bool isValid { get; private set; }
			public bool isConnected { get { Update(); return pose.bDeviceIsConnected; } }
			public bool hasTracking { get { Update(); return pose.bPoseIsValid; } }

			public bool isOutOfRange { get { Update(); return pose.eTrackingResult == ETrackingResult.Running_OutOfRange || pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange; } }
			public bool isCalibrating { get { Update(); return pose.eTrackingResult == ETrackingResult.Calibrating_InProgress || pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange; } }
			public bool isUninitialized { get { Update(); return pose.eTrackingResult == ETrackingResult.Uninitialized; } }

			public void Update()
			{
				if (Time.frameCount != prevFrameCount)
				{
					prevFrameCount = Time.frameCount;
					prevState = state;

					var system = OpenVR.System;
					if (system != null)
					{
						isValid = system.GetControllerStateWithPose(ETrackingUniverseOrigin.TrackingUniverseStanding, deviceIndex, ref state, (uint)Marshal.SizeOf(state), ref pose);
					}
				}
			}

			public bool GetPress(ulong buttonMask) { Update(); return (state.ulButtonPressed & buttonMask) != 0; }
			public bool GetPressDown(ulong buttonMask) { Update(); return (state.ulButtonPressed & buttonMask) != 0 && (prevState.ulButtonPressed & buttonMask) == 0; }
			public bool GetPressUp(ulong buttonMask) { Update(); return (state.ulButtonPressed & buttonMask) == 0 && (prevState.ulButtonPressed & buttonMask) != 0; }

			public bool GetPress(EVRButtonId buttonId) { return GetPress(1ul << (int)buttonId); }
			public bool GetPressDown(EVRButtonId buttonId) { return GetPressDown(1ul << (int)buttonId); }
			public bool GetPressUp(EVRButtonId buttonId) { return GetPressUp(1ul << (int)buttonId); }

			public bool GetTouch(ulong buttonMask) { Update(); return (state.ulButtonTouched & buttonMask) != 0; }
			public bool GetTouchDown(ulong buttonMask) { Update(); return (state.ulButtonTouched & buttonMask) != 0 && (prevState.ulButtonTouched & buttonMask) == 0; }
			public bool GetTouchUp(ulong buttonMask) { Update(); return (state.ulButtonTouched & buttonMask) == 0 && (prevState.ulButtonTouched & buttonMask) != 0; }

			public bool GetTouch(EVRButtonId buttonId) { return GetTouch(1ul << (int)buttonId); }
			public bool GetTouchDown(EVRButtonId buttonId) { return GetTouchDown(1ul << (int)buttonId); }
			public bool GetTouchUp(EVRButtonId buttonId) { return GetTouchUp(1ul << (int)buttonId); }
		}

		private static Device[] devices;

		public static Device Input(int deviceIndex)
		{
			if (devices == null)
			{
				devices = new Device[OpenVR.k_unMaxTrackedDeviceCount];
				for (uint i = 0; i < devices.Length; i++)
					devices[i] = new Device(i);
			}

			return devices[deviceIndex];
		}

		public static void Update()
		{
			for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
				Input(i).Update();
		}

		// This helper can be used in a variety of ways.  Beware that indices may change
		// as new devices are dynamically added or removed, controllers are physically
		// swapped between hands, arms crossed, etc.
		public enum DeviceRelation
		{
			First,
			// radially
			Leftmost,
			Rightmost,
			// distance - also see vr.hmd.GetSortedTrackedDeviceIndicesOfClass
			FarthestLeft,
			FarthestRight,
		}

		public static int GetDeviceIndex(DeviceRelation relation,
			ETrackedDeviceClass deviceClass = ETrackedDeviceClass.Controller,
			int relativeTo = (int)OpenVR.k_unTrackedDeviceIndex_Hmd) // use -1 for absolute tracking space
		{
			var result = -1;

			var invXform = ((uint)relativeTo < OpenVR.k_unMaxTrackedDeviceCount) ?
				Input(relativeTo).transform.GetInverse() : SteamVR_Utils.RigidTransform.identity;

			var system = OpenVR.System;
			if (system == null)
				return result;

			var best = -float.MaxValue;
			for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
			{
				if (i == relativeTo || system.GetTrackedDeviceClass((uint)i) != deviceClass)
					continue;

				var device = Input(i);
				if (!device.isConnected)
					continue;

				if (relation == DeviceRelation.First)
					return i;

				float score;

				var pos = invXform * device.transform.pos;
				if (relation == DeviceRelation.FarthestRight)
				{
					score = pos.x;
				}
				else if (relation == DeviceRelation.FarthestLeft)
				{
					score = -pos.x;
				}
				else
				{
					var dir = new Vector3(pos.x, 0.0f, pos.z).normalized;
					var dot = Vector3.Dot(dir, Vector3.forward);
					var cross = Vector3.Cross(dir, Vector3.forward);
					if (relation == DeviceRelation.Leftmost)
					{
						score = (cross.y > 0.0f) ? 2.0f - dot : dot;
					}
					else
					{
						score = (cross.y < 0.0f) ? 2.0f - dot : dot;
					}
				}

				if (score > best)
				{
					result = i;
					best = score;
				}
			}

			if (result != -1)
            {
				Debug.Log("Found device with relation " + relation + " and index " + result);
            }

			return result;
		}
	}
}
