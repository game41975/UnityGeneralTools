using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectBase : ScriptableObject
{
    //public abstract void InitParam<T>(T param) where T : ScriptableObjectParameterBase;
    public virtual void InitParam(List<List<string>> list) 
    {
    }

    public bool ConvertParam<T>(string key, System.Action<T> action)
    {
        bool convertSuccessed = false;
        var type = typeof(T);
        switch (System.Type.GetTypeCode(type))
        {
            case TypeCode.Int32:
                {
                    convertSuccessed = int.TryParse(key, out int result);
                    if (convertSuccessed)
                    {
                        action.Invoke((T)(object)result);
                    }
                }
                
                break;
            case TypeCode.Single:
                {
                    convertSuccessed = float.TryParse(key, out float result);
                    if (convertSuccessed)
                    {
                        action.Invoke((T)(object)result);
                    }
                }
                break;
            case TypeCode.Double:
                {
                    convertSuccessed = double.TryParse(key, out double result);
                    if (convertSuccessed)
                    {
                        action.Invoke((T)(object)result);
                    }
                }
                break;
            case TypeCode.String:
                convertSuccessed = true;
                action.Invoke((T)(object)key);
                break;
            default:
                //対応外の型
                break;
        }

        if (!convertSuccessed)
        {
            //TODO:変換失敗時のログ表示
        }

        return convertSuccessed;
    }
}

public class ScriptableObjectParameterBase
{
}