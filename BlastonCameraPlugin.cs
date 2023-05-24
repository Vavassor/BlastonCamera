﻿/*
Copyright 2019 LIV inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.IO;
using System;
using UnityEngine;
using BlastonCameraBehaviour.Config;
using Valve.VR;

namespace BlastonCameraBehaviour
{

    // User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
    // Only public variables will be serialized.
    public class BlastonCameraPluginSettings : IPluginSettings
    {
        public float fov = 65f;
        public float distance = 2f;
        public float speed = 1f;
    }

    // The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
    public class BlastonCameraPlugin : IPluginCameraBehaviour
    {
        // Store your settings localy so you can access them.
        BlastonCameraPluginSettings _settings = new BlastonCameraPluginSettings();

        // Provide your own settings to store user defined settings .   
        public IPluginSettings settings => _settings;

        // Invoke ApplySettings event when you need to save your settings.
        // Do not invoke event every frame if possible.
        public event EventHandler ApplySettings;

        // ID is used for the camera behaviour identification when the behaviour is selected by the user.
        // It has to be unique so there are no plugin collisions.
        public string ID => "BlastonCameraPlugin";
        // Readable plugin name "Keep it short".
        public string name => "Blaston Camera";
        // Author name.
        public string author => "FaintBeep";
        // Plugin version.
        public string version => "1.0";

        // Localy store the camera helper provided by LIV.
        PluginCameraHelper _helper;
        Config.Config config;

        // Constructor is called when plugin loads
        public BlastonCameraPlugin()
        {
        }

        // OnActivate function is called when your camera behaviour was selected by the user.
        // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
        public void OnActivate(PluginCameraHelper helper)
        {
            _helper = helper;
            string filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LIV/Plugins/CameraBehaviours/BlastonCamera.json");
            try
            {
                config = ConfigFileLoader.LoadFile(new PlayerHelper(helper), filepath);
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private bool GetPressDown(Controller.DeviceRelation deviceRelation, ulong buttonMask)
        {
            var deviceIndex = Controller.GetDeviceIndex(deviceRelation);
            if (deviceIndex != -1 && Controller.Input(deviceIndex).GetPressDown(buttonMask))
            {
                return true;
            }

            return false;
        }

        private bool GetPressDown(Controller.DeviceRelation deviceRelation, EVRButtonId buttonId)
        {
            var deviceIndex = Controller.GetDeviceIndex(deviceRelation);
            if (deviceIndex != -1 && Controller.Input(deviceIndex).GetPressDown(buttonId))
            {
                return true;
            }

            return false;
        }


        private string GetStringProperty(CVRSystem system, uint deviceIndex, ETrackedDeviceProperty prop)
        {
            var error = ETrackedPropertyError.TrackedProp_Success;
            var capacity = system.GetStringTrackedDeviceProperty(deviceIndex, prop, null, 0, ref error);

            if (capacity > 1)
            {
                var result = new System.Text.StringBuilder((int) capacity);
                system.GetStringTrackedDeviceProperty(deviceIndex, prop, result, capacity, ref error);
                return result.ToString();
            }

            return (error != ETrackedPropertyError.TrackedProp_Success) ? error.ToString() : "<unknown>";
        }

        // OnSettingsDeserialized is called only when the user has changed camera profile or when the.
        // last camera profile has been loaded. This overwrites your settings with last data if they exist.
        public void OnSettingsDeserialized()
        {

        }

        // OnFixedUpdate could be called several times per frame. 
        // The delta time is constant and it is ment to be used on robust physics simulations.
        public void OnFixedUpdate()
        {

        }

        // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
        // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
        // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
        public void OnUpdate()
        {
            if (config.motions != null)
            {
                Transform camera = config.motions["default"].Transform(Time.deltaTime);
                _helper.UpdateCameraPose(camera.position, camera.rotation, _settings.fov);
            }

            if (GetPressDown(Controller.DeviceRelation.Leftmost, Controller.ButtonMask.Trigger))
            {
                Debug.Log("Left trigger pressed!");
            }

            if (GetPressDown(Controller.DeviceRelation.Leftmost, Controller.ButtonMask.Grip))
            {
                Debug.Log("Left grip pressed!");
            }

            if (GetPressDown(Controller.DeviceRelation.Leftmost, EVRButtonId.k_EButton_A))
            {
                Debug.Log("Left A pressed!");
            }

            if (GetPressDown(Controller.DeviceRelation.Rightmost, Controller.ButtonMask.Trigger))
            {
                Debug.Log("Right trigger pressed!");
            }

            if (GetPressDown(Controller.DeviceRelation.Rightmost, Controller.ButtonMask.Grip))
            {
                Debug.Log("Right grip pressed!");
            }
        }

        // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
        public void OnLateUpdate()
        {

        }

        // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
        // The camera behaviour should clean everything it created when the behaviour is deactivated.
        public void OnDeactivate()
        {
            // Saving settings here
            ApplySettings?.Invoke(this, EventArgs.Empty);
        }

        // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
        // This is the last chance to clean after your self.
        public void OnDestroy()
        {

        }
    }
}