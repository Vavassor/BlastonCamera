using System;
using System.Runtime.InteropServices;
using Valve.VR;
using UnityEngine;

namespace BlastonCameraBehaviour
{
    class ActionController
    {
        ulong defaultActionSet = OpenVR.k_ulInvalidActionSetHandle;
        ulong grabPinchHandle = OpenVR.k_ulInvalidActionHandle;
        ulong grabGripHandle = OpenVR.k_ulInvalidActionHandle;

        VRActiveActionSet_t[] activeActionSets;

        public enum ActionType
        {
            Grip,
            Pinch,
        }

        public void Init()
        {
            if (OpenVR.Input == null)
            {
                Debug.LogError("OpenVR.Input is not available.");
                return;
            }

            OpenVR.Input.GetActionHandle("/actions/default/in/grabpinch", ref grabPinchHandle);
            OpenVR.Input.GetActionHandle("/actions/default/in/grabgrip", ref grabGripHandle);

            OpenVR.Input.GetActionSetHandle("/actions/default", ref defaultActionSet);

            activeActionSets = new VRActiveActionSet_t[1];
            activeActionSets[0].ulActionSet = defaultActionSet;
            activeActionSets[0].ulRestrictedToDevice = OpenVR.k_ulInvalidInputValueHandle;
            activeActionSets[0].ulSecondaryActionSet = OpenVR.k_ulInvalidActionSetHandle;
        }

        public void Update()
        {
            if (OpenVR.Input == null)
            {
                return;
            }

            OpenVR.Input.UpdateActionState(activeActionSets, (uint) Marshal.SizeOf(activeActionSets[0]));
        }

        public bool IsActionEnd(ActionType actionType)
        {
            var actionData = GetActionData(actionType);
            if (actionData.bChanged)
            {
                Debug.Log("Action changed - origin: " + GetOriginLocalizedName(actionData.activeOrigin) + " state: " + actionData.bState + " updateTime: " + actionData.fUpdateTime);
            }
            return !actionData.bState && actionData.bChanged;
        }

        public bool IsActionStart(ActionType actionType)
        {
            var actionData = GetActionData(actionType);
            if (actionData.bChanged)
            {
                Debug.Log("Action changed - origin: " + GetOriginLocalizedName(actionData.activeOrigin) + " state: " + actionData.bState + " updateTime: " + actionData.fUpdateTime);
            }
            return actionData.bState && actionData.bChanged;
        }

        private ulong GetActionHandleByActionType(ActionType actionType)
        {
            switch(actionType)
            {
                case ActionType.Grip:
                    return grabGripHandle;
                case ActionType.Pinch:
                    return grabPinchHandle;
                default:
                    return OpenVR.k_ulInvalidActionHandle;
            }
        }

        private string GetOriginLocalizedName(ulong origin)
        {
            int capacity = 64;
            var result = new System.Text.StringBuilder(capacity);
            var error = OpenVR.Input.GetOriginLocalizedName(origin, result, (uint) capacity, 1);
            return (error == EVRInputError.None) ? result.ToString() : "Unknown";
        }

        private InputDigitalActionData_t GetActionData(ActionType actionType)
        {
            ulong actionHandle = GetActionHandleByActionType(actionType);

            InputDigitalActionData_t actionData = new InputDigitalActionData_t();
            OpenVR.Input.GetDigitalActionData(actionHandle, ref actionData, (uint) Marshal.SizeOf(actionData), OpenVR.k_ulInvalidInputValueHandle);

            return actionData;
        }
    }
}
