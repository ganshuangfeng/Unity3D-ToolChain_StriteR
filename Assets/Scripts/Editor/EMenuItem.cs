﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TEditor
{
    public static class EMenuItem
    {
        [MenuItem("Work Flow/Take Screen Shot _F12", false, 102)]
        static void TakeScreenShot()
        {
            DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/ScreenShots");
            string path = Path.Combine(directory.Parent.FullName, string.Format("Screenshot_{0}.png", DateTime.Now.ToString("yyyyMMdd_Hmmss")));
            Debug.LogFormat("ScreenShot Successful:\n<Color=#F1F635FF>{0}</Color>",path);
            ScreenCapture.CaptureScreenshot(path);
        }

        [MenuItem("Work Flow/AssetBundles/Test Asset Bundle Constructing", false, 110)]
        static void BuildAllAssetBundlesAndroid()
        {
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.None, BuildTarget.Android);
        }

        [MenuItem("Work Flow/UI Tools/Missing Fonts Replacer", false, 203)]
        static void ShowWindow() => EditorWindow.GetWindow<EUIFontsMissingReplacerWindow>().Show();

        [MenuItem("Work Flow/Art/(Optimize)Animation Instance Baker", false, 300)]
        static void ShowOptimizeWindow() => EditorWindow.GetWindow(typeof(EAnimationInstanceBakerWindow));

        [MenuItem("Work Flow/Art/Plane Mesh Generator", false, 301)]
        static void ShowPlaneGenerator() => EditorWindow.GetWindow(typeof(EPlaneMeshGeneratorWindow));
    }

}