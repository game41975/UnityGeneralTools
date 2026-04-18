using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace DataImporter
{
    public class DataImportSupport
    {
        public static bool ConvertParam<T>(string key, System.Action<T> action)
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

        public static List<List<string>> LoadCsv(string path)
        {
            List<List<string>> csvParamList = new List<List<string>>();

            StreamReader reader = new StreamReader(path);
            if (!reader.IsUnityNull())
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    csvParamList.Add(new List<string>());
                    foreach(var value in values)
                    {
                        csvParamList[csvParamList.Count - 1].Add(value);
                    }
                }
            }

            return csvParamList;
        }
    }
}