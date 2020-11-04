﻿
using System;
using UnityEditor;
using UnityEngine;
namespace Rendering.ImageEffect
{
    public abstract class AImageEffectBase
    {
        public virtual void OnImageProcess(RenderTexture src, RenderTexture dst) { }
        public abstract void DoValidate();
        public virtual void OnDestroy() { }

        public static Material CreateMaterial(Type _type)
        {
            Shader _shader = Shader.Find("Hidden/" + _type.Name);

            if (_shader == null)
                throw new NullReferenceException("Invalid ImageEffect Shader Found:" + _type.Name);

            if (!_shader.isSupported)
                throw new NullReferenceException("Shader Not Supported:" + _type.Name);

            return new Material(_shader) { name = _type.Name, hideFlags = HideFlags.HideAndDontSave };
        }

    }
    public class ImageEffectBase<T>:AImageEffectBase where T:ImageEffectParamBase
    {
        public Material m_Material { get; private set; }
        protected virtual string m_ShaderLocation => "Override This Please";
        Func<T> GetParamsFunc;
        public T GetParams() => GetParamsFunc();
        public ImageEffectBase(Func<T> _GetParams)
        {
            m_Material = CreateMaterial(this.GetType()); 
            GetParamsFunc = _GetParams;
        }

        public virtual void OnDestory()
        {
            if (m_Material)
                GameObject.DestroyImmediate(m_Material);

            m_Material = null;
            GetParamsFunc = null;
        }
        public override void OnImageProcess( RenderTexture src, RenderTexture dst)
        {
            if (m_Material != null)
                Graphics.Blit(src, dst, m_Material);
            else
                Graphics.Blit(src, dst);
        }

        public override void DoValidate()
        {
            T param = GetParams();
            if (param==null)
                return;
            OnValidate(GetParams());
        }
        protected virtual void OnValidate(T _params)
        {

        }
    }

    public class ImageEffectParamBase
    {
        public static readonly ImageEffectParamBase m_Default = new ImageEffectParamBase();
    }


    [ExecuteInEditMode]
    public class PostEffectBase<T> : MonoBehaviour where T:AImageEffectBase
    {
        protected T m_Effect { get; private set; }
        protected virtual T OnGenerateRequiredImageEffects() => throw new Exception("Override This Please");

        protected virtual void Awake()
        {
            OnValidate();
        }
        protected virtual void OnDestroy()
        {
            Destroy();
        }
        public virtual void OnValidate()
        {
            if (m_Effect == null)
                m_Effect = OnGenerateRequiredImageEffects();

            m_Effect.DoValidate();
        }

        void Destroy()
        {
            if (m_Effect == null)
                return;

            m_Effect.OnDestroy();
            m_Effect = null;
        }
        public void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (m_Effect == null)
            {
                Graphics.Blit(src, dst);
                return;
            }

#if UNITY_EDITOR
            //保存场景时Material会重置 需要重新设置属性
            m_Effect.DoValidate();
#endif

            m_Effect.OnImageProcess(src, dst);
        }
    }
}