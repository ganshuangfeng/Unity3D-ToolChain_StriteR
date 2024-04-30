﻿
using UnityEngine;

namespace UnityEditor.Extensions.ScriptableObjectBundle
{
    public interface IAssetPipelineProcessContinuous
    {
        public bool Executing();
        public float process { get; }
        public void Cancel();
        public void End();
        public void OnGUI();
    }
    
    public abstract class EAssetPipelineProcess : AScriptableObjectBundleElement
    {
        public abstract void OnExecute();
    }
}