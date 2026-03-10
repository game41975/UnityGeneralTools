using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

[Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ReadOnlyAttribute : PropertyAttribute
{
    
}