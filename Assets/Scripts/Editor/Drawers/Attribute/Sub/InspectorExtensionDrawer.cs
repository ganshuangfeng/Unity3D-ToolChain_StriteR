﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Extensions
{
    [CustomPropertyDrawer(typeof(InspectorExtensionAttribute))]
    public class InspectorExtensionDrawer : ASubAttributePropertyDrawer<InspectorExtensionAttribute>
    {
        private object target;
        private List<ButtonAttributeData> inspectorMethods;
        private const int kPadding = 1;
        private const int kParameterHeight = 20;
        private const int kMethodButtonHeight = 20;
        private const int kTitleHeight = 18;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            target = property.GetFieldValue();
            inspectorMethods ??= UInspectorExtension.GetInspectorMethods(target);
            var height = EditorGUI.GetPropertyHeight(property, label, true);

            if (height < 20)
                return height;
            
            foreach (var method in inspectorMethods)
            {
                if(!method.attribute.IsElementVisible(target))
                    continue;
                
                height += kPadding;
                if (method.parameters.Length == 0)
                {
                    height += kMethodButtonHeight;
                    continue;
                }
                
                height += kTitleHeight;
                height += method.parameters.Length * kParameterHeight;
                height += kMethodButtonHeight;
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyHeight = EditorGUI.GetPropertyHeight(property, label);
            position = position.ResizeY(propertyHeight);
            EditorGUI.PropertyField(position, property, label, true);
            if (propertyHeight < 20)
                return;

            position = position.MoveX(25).ResizeX(position.size.x - 25);
            foreach (var data in inspectorMethods)
            {
                if(!data.attribute.IsElementVisible(target))
                    continue;
                
                position = position.MoveY().ResizeY(kPadding);
                if (data.parameters.Length == 0)
                {
                    position = position.MoveY().ResizeY(kMethodButtonHeight);
                    if (GUI.Button(position, data.method.Name))
                        data.method.Invoke(target,data.parameters.Select(p=>p.value).ToArray());
                    continue;
                }
                
                position = position.MoveY().ResizeY(kTitleHeight);
                EditorGUI.LabelField(position,data.method.Name, EditorStyles.boldLabel);

                foreach (var parameter in data.parameters)
                {
                    position = position.MoveY().ResizeY(kParameterHeight);
                    var key = parameter.type;
                    switch (UInspectorExtension.LookUp(key))
                    {
                        case EButtonParameters.Float: parameter.value = EditorGUI.FloatField(position,parameter.name,(float)parameter.value); break;
                        case EButtonParameters.Integer: parameter.value = EditorGUI.IntField(position,parameter.name,(int)parameter.value); break;
                        case EButtonParameters.String: parameter.value = EditorGUI.TextField(position,parameter.name,(string)parameter.value); break;
                        case EButtonParameters.Vector3: parameter.value = EditorGUI.Vector3Field(position,parameter.name,(Vector3)parameter.value); break;
                        case EButtonParameters.Object: parameter.value = EditorGUI.ObjectField(position,(Object)parameter.value,parameter.type,true); break;
                        default: case EButtonParameters.NotSupported: EditorGUI.LabelField(position,$"Not Supported Type {key}");break;
                    }
                }
                
                position = position.MoveY().ResizeY(kMethodButtonHeight);
                if (GUI.Button(position, "Execute"))
                    data.method.Invoke(target,data.parameters.Select(p=>p.value).ToArray());
            }
        }
    }
}