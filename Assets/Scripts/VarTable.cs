using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public enum VarType
{
    Field,
    Property
}

[System.Serializable]
public class VarTableVar {
    public Type type;
    public string typeName;
    public string fieldName;
    public Component target;
    public VarType varType;

    public static Dictionary<object, bool> enableToggle = new Dictionary<object, bool>();

    static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }

    public static object EditorField(object val, Type type, out bool isDirty)
    {
        isDirty = false;
        if (type == typeof(int))
        {
            return EditorGUILayout.IntField((int)val);
        }
        else if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle((bool)val);
        }
        else if (type == typeof(float))
        {
            return EditorGUILayout.FloatField((float)val);
        }
        else if (type == typeof(string))
        {
            return EditorGUILayout.TextField((string)val);
        }
        else if (type == typeof(Color))
        {
            return EditorGUILayout.ColorField((Color)val);
        }
        else if (type == typeof(Vector2))
        {
            return EditorGUILayout.Vector2Field("", (Vector2)val);
        }
        else if (type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field("", (Vector3)val);
        }
        else if (type == typeof(Vector4))
        {
            return EditorGUILayout.Vector4Field("", (Vector4)val);
        }
        else if (type.IsEnum)
        {
            return EditorGUILayout.EnumPopup((Enum)val);
        }
        else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            return EditorGUILayout.ObjectField(val as UnityEngine.Object, type, true);
        }
        else if (type.IsArray && type.GetArrayRank() == 1)
        {
            Array arrCast = val as Array;
            Type arrayType = type.GetElementType();

            int count = arrCast.Length;

            if (!enableToggle.ContainsKey(val))
            {
                enableToggle.Add(val, false);
            }

            EditorGUILayout.BeginVertical();

            enableToggle[val] = EditorGUILayout.Foldout(enableToggle[val], "Array");
            if (enableToggle[val])
            {
                int newCount = EditorGUILayout.IntField(count);

                if (newCount != count)
                {
                    enableToggle.Remove(val);
                    Array newArr = Array.CreateInstance(arrayType, newCount);
                    enableToggle.Add(newArr, true);

                    for (int i = 0; i < Mathf.Min(newCount, count); i++)
                    {
                        newArr.SetValue(arrCast.GetValue(i), i);
                    }

                    return newArr;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        bool isElemDirty = false;
                        object oldVal = arrCast.GetValue(i);
                        object newVal = EditorField(oldVal, arrayType, out isElemDirty);
                        arrCast.SetValue(newVal, i);

                        isDirty |= isElemDirty || !oldVal.Equals(newVal);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            return val;
        }
        else
        {
            Type[] interfaces = type.GetInterfaces();

            bool isEnumerable = false;

            Type enumerableType = typeof(object);

            foreach (Type _int in interfaces)
            {
                if (_int.IsGenericType && _int.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    enumerableType = _int.GetGenericArguments()[0];
                    isEnumerable = true;
                    break;
                }
            }

            if (isEnumerable)
            {
                IList listCast = val as IList;

                int count = listCast.Count;

                if (!enableToggle.ContainsKey(val))
                {
                    enableToggle.Add(val, false);
                }

                EditorGUILayout.BeginVertical();

                enableToggle[val] = EditorGUILayout.Foldout(enableToggle[val], "List");
                if (enableToggle[val])
                {
                    int newCount = EditorGUILayout.IntField(count);

                    if (newCount < count)
                    {
                        for (int i = newCount; i < count; i++)
                        {
                            listCast.RemoveAt(newCount);
                        }
                    }
                    else if (newCount > count)
                    {
                        for (int i = count; i < newCount; i++)
                        {
                            listCast.Add(GetDefaultValue(enumerableType));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            bool isElemDirty = false;
                            object oldVal = listCast[i];
                            object newVal = EditorField(listCast[i], enumerableType, out isElemDirty);
                            listCast[i] = newVal;

                            isDirty |= isElemDirty || !oldVal.Equals(newVal);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            return val;
        }
    }

    public virtual bool Editor() { return false; }
}

[System.Serializable]
public class VarTableField : VarTableVar
{
    public override bool Editor()
    {
        Type monoType = target.GetType();
        FieldInfo targetField = monoType.GetField(fieldName);
        if (targetField == null)
        {
            return true;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            return true;
        }

        EditorGUILayout.LabelField(target.gameObject.name + "." + monoType.Name + "." + targetField.Name);

        bool isDirty = false;
        object currVal = targetField.GetValue(target);
        object newVal = VarTableVar.EditorField(currVal, type, out isDirty);

        if (!newVal.Equals(currVal) || isDirty)
        {
            targetField.SetValue(target, newVal);
            EditorSceneManager.MarkAllScenesDirty();
        }

        EditorGUILayout.EndHorizontal();

        return false;
    }
}

[System.Serializable]
public class VarTableProperty : VarTableVar
{
    public override bool Editor()
    {
        Type monoType = target.GetType();
        PropertyInfo targetProp = monoType.GetProperty(fieldName);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            return true;
        }

        EditorGUILayout.LabelField(target.gameObject.name + "." + monoType.Name + "." + targetProp.Name);

        bool isDirty = false;
        object currVal = targetProp.GetValue(target, null);
        object newVal = VarTableVar.EditorField(currVal, type, out isDirty);

        if (!newVal.Equals(currVal) || isDirty)
        {
            targetProp.SetValue(target, newVal, null);
            EditorSceneManager.MarkAllScenesDirty();
        }

        EditorGUILayout.EndHorizontal();

        return false;
    }
}
public class VarTable : MonoBehaviour, ISerializationCallbackReceiver
{
    public List<VarTableVar> tableVars = new List<VarTableVar>();

    public void OnAfterDeserialize()
    {
        for (int i = 0; i < tableVars.Count; i++)
        {
            VarTableVar vtVar = tableVars[i];
            vtVar.type = Type.GetType(vtVar.typeName);

            if (vtVar.varType == VarType.Field)
            {
                VarTableField varField = new VarTableField();
                varField.fieldName = vtVar.fieldName;
                varField.type = vtVar.type;
                varField.target = vtVar.target;
                
                tableVars.RemoveAt(i);
                tableVars.Insert(i, varField);
            }
            else if(vtVar.varType == VarType.Property)
            {
                VarTableProperty propField = new VarTableProperty();
                propField.fieldName = vtVar.fieldName;
                propField.type = vtVar.type;
                propField.target = vtVar.target;

                tableVars.RemoveAt(i);
                tableVars.Insert(i, propField);
            }
        }
    }

    public void OnBeforeSerialize()
    {
        foreach(VarTableVar vtVar in tableVars)
        {
            vtVar.typeName = vtVar.type.AssemblyQualifiedName;
            if(vtVar is VarTableField)
            {
                vtVar.varType = VarType.Field;
            }
            else if(vtVar is VarTableProperty)
            {
                vtVar.varType = VarType.Property;
            }
        }
    }
}
