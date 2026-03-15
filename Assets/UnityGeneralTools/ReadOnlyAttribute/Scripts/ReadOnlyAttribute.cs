using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ReadOnlyAttribute
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {

    }
}

