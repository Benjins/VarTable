using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(VarTable))]
public class VarTableEditor : Editor
{
    GameObject targetObj;
    Component targetComp;

    string[] fieldNames;
    int compIndex = 0;

    string[] propertyNames;

    Component[] comps;
    int fieldAndPropertyIndex = 0;

    public override void OnInspectorGUI()
    {
        VarTable myTarget = (VarTable)target;

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();

        GameObject newTargetObj = (GameObject)EditorGUILayout.ObjectField(targetObj, typeof(GameObject), true);

        if (targetObj != newTargetObj)
        {
            targetComp = null;
            compIndex = 0;
            comps = null;
            targetObj = newTargetObj;
        }

        if (targetObj != null)
        {

            if (comps == null)
            {
                comps = targetObj.GetComponents(typeof(UnityEngine.Component));
            }

            string[] compNames = new string[comps.Length];

            for (int i = 0; i < comps.Length; i++)
            {
                compNames[i] = comps[i].GetType().Name;
            }

            int newCompIndex = EditorGUILayout.Popup(compIndex, compNames);

            if (newCompIndex != compIndex)
            {
                fieldNames = null;
                propertyNames = null;
            }

            compIndex = newCompIndex;

            targetComp = comps[compIndex];

            if (targetComp != null)
            {
                if (fieldNames == null)
                {
                    Type targetType = targetComp.GetType();
                    FieldInfo[] fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                    fieldNames = new string[fields.Length];

                    for (int i = 0; i < fields.Length; i++)
                    {
                        fieldNames[i] = fields[i].Name;
                    }
                }

                if (propertyNames == null)
                {
                    Type targetType = targetComp.GetType();
                    PropertyInfo[] properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    propertyNames = new string[properties.Length];

                    for (int i = 0; i < properties.Length; i++)
                    {
                        int offset = properties.Length - propertyNames.Length;
                        if (properties[i].CanWrite)
                        {
                            propertyNames[i - offset] = properties[i].Name;
                        }
                        else
                        {
                            string[] newProps = new string[propertyNames.Length - 1];
                            for (int j = 0; j < propertyNames.Length - 1; j++)
                            {
                                newProps[j] = propertyNames[j];
                            }
                            propertyNames = newProps;
                        }
                    }
                }

                string[] fieldAndPropertyNames = new string[fieldNames.Length + propertyNames.Length];
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    fieldAndPropertyNames[i] = fieldNames[i];
                }

                for (int i = 0; i < propertyNames.Length; i++)
                {
                    fieldAndPropertyNames[i + fieldNames.Length] = propertyNames[i];
                }

                fieldAndPropertyIndex = EditorGUILayout.Popup(fieldAndPropertyIndex, fieldAndPropertyNames);

                if (GUILayout.Button("Add"))
                {
                    if (fieldAndPropertyIndex < fieldNames.Length)
                    {
                        VarTableField newVar = new VarTableField();
                        newVar.fieldName = fieldNames[fieldAndPropertyIndex];
                        newVar.target = targetComp;
                        newVar.type = targetComp.GetType().GetField(newVar.fieldName).FieldType;

                        myTarget.tableVars.Add(newVar);

                        EditorSceneManager.MarkAllScenesDirty();
                    }
                    else
                    {
                        VarTableProperty newProp =  new VarTableProperty();
                        newProp.fieldName = propertyNames[fieldAndPropertyIndex - fieldNames.Length];
                        newProp.target = targetComp;
                        newProp.type = targetComp.GetType().GetProperty(newProp.fieldName).PropertyType;

                        myTarget.tableVars.Add(newProp);

                        EditorSceneManager.MarkAllScenesDirty();
                    }

                    /*
                    targetObj = null;
                    targetComp = null;
                    fieldNames = null;
                    compNames = null;

                    fieldAndPropertyIndex = 0;
                    compIndex = 0;
                    */
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < myTarget.tableVars.Count; i++)
        {
            VarTableVar tableVar = myTarget.tableVars[i];
            if (tableVar.target != null && tableVar.type != null)
            {
                bool shouldRemove = tableVar.Editor();

                if (shouldRemove)
                {
                    myTarget.tableVars.RemoveAt(i);
                    i--;
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    
}